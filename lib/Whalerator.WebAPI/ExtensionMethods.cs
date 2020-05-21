/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Queue;
using Whalerator.Security;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public static class ExtensionMethods
    {
        public static RegistryCredentials ToRegistryCredentials(this ClaimsPrincipal principal)
        {
            return new RegistryCredentials
            {
                Registry = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Registry)))?.Value,
                Username = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Username)))?.Value,
                Password = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Password)))?.Value
            };
        }

        public static ClaimsIdentity ToClaimsIdentity(this RegistryCredentials credentials)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(nameof(RegistryCredentials.Registry), credentials.Registry, ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(credentials.Username)) { claims.Add(new Claim(nameof(RegistryCredentials.Username), credentials.Username, ClaimValueTypes.String)); }
            if (!string.IsNullOrEmpty(credentials.Password)) { claims.Add(new Claim(nameof(RegistryCredentials.Password), credentials.Password, ClaimValueTypes.String)); }

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }

        public static IServiceCollection AddWhaleRegistry(this IServiceCollection services, ConfigRoot config, PublicConfig uiConfig, ILogger logger)
        {
            var staticTtl = config.Cache.StaticTtl == 0 ? (TimeSpan?)null : new TimeSpan(0, 0, config.Cache.StaticTtl);
            var volatileTtl = new TimeSpan(0, 0, config.Cache.VolatileTtl);
            logger?.LogInformation($"Cache lifetime for static registry objects: {(staticTtl == null ? "unlimited" : staticTtl.ToString())}");
            logger?.LogInformation($"Cache lifetime for volatile registry objects: {volatileTtl}");

            Func<string, IAuthHandler, IDistributionClient> factory;
            if (string.IsNullOrEmpty(config.Catalog.RegistryRoot))
            {
                services.AddScoped<IDistributionClient, DistributionClient>();
                factory = (host, handler) => new DistributionClient(handler) { Host = host };
            }
            else
            {
                services.AddScoped<IDistributionClient, LocalDistributionClient>();
                factory = (host, handler) => new LocalDistributionClient(config);
            }


            services.AddSingleton(p =>
            {
                var catalogHandler = string.IsNullOrEmpty(config.Catalog?.User?.Username) ? null : p.GetService<IAuthHandler>();
                catalogHandler?.Login(config.Catalog.Registry, config.Catalog.User.Username, config.Catalog.User.Password);

                return new RegistrySettings
                {
                    DistributionFactory = factory,
                    AuthHandler = p.GetService<IAuthHandler>(),
                    CacheFactory = p.GetService<ICacheFactory>(),
                    CatalogAuthHandler = catalogHandler,
                    HiddenRepos = config.Catalog?.Hidden,
                    LayerCache = config.ContentScanner?.LayerCache,
                    StaticRepos = config.Catalog?.Repositories,
                    StaticTtl = staticTtl,
                    VolatileTtl = volatileTtl
                };
            });

            services.AddScoped<IClientFactory, ClientFactory>();


            uiConfig.Registry = config.Catalog?.Registry;
            uiConfig.AutoLogin = config.Catalog?.AutoLogin ?? false;

            return services;
        }

        public static IServiceCollection AddWhaleSerialization(this IServiceCollection services)
        {

            // System.Text.Json is a bit limited, so rather than have two different Json libraries in use just revert to Newtonsoft for now
            services.AddMvc().AddNewtonsoftJson().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.IgnoreNullValues = true;
            });

            return services;
        }

        public static IServiceCollection AddWhaleCrypto(this IServiceCollection services, ConfigRoot config, ILogger logger)
        {
            RSA crypto;
            var keyFile = config.Security?.PrivateKey;
            if (!string.IsNullOrEmpty(keyFile) && File.Exists(keyFile))
            {
                logger?.LogInformation($"Loading private key from {config.Security.PrivateKey}.");
                crypto = new RSA(File.ReadAllText(keyFile));
            }
            else
            {
                logger?.LogInformation($"Generating temporary private key.");
                crypto = new RSA(2048);
            }
            services.AddSingleton<ICryptoAlgorithm>(crypto);

            return services;
        }

        public static IServiceCollection AddWhaleAuth(this IServiceCollection services)
        {
            services.AddSingleton<RegistryAuthenticationDecoder>();
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Bearer";
                o.DefaultChallengeScheme = "Bearer";
                o.DefaultForbidScheme = "Bearer";
            }).AddScheme<AuthenticationSchemeOptions, RegistryAuthenticationHandler>("Bearer", o => { });

            services.AddScoped(p => p.GetService<ICacheFactory>().Get<Authorization>());
            services.AddTransient<IAuthHandler, AuthHandler>();

            return services;
        }

        public static IServiceCollection AddWhaleCache(this IServiceCollection services, ConfigRoot config, ILogger logger)
        {
            if (string.IsNullOrEmpty(config.Cache.Redis))
            {
                logger?.LogInformation("Using in-memory cache.");
                services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions { }));
                services.AddScoped<ICacheFactory>(provider => new MemCacheFactory(provider.GetService<IMemoryCache>()));
            }
            else
            {
                logger?.LogInformation($"Using Redis cache ({config.Cache.Redis})");
                var ready = false;
                var retryTime = 15;
                while (!ready)
                {
                    try
                    {
                        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(config.Cache.Redis));
                        ready = true;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning($"Could not connect to redis instance. Retrying in {retryTime}s.");
                        logger?.LogInformation($"Connection string: {config.Cache.Redis}");
                        logger?.LogInformation(ex, "Error:");
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(retryTime));
                    }
                }

                services.AddScoped<ICacheFactory>(p => new RedCacheFactory { Mux = p.GetService<IConnectionMultiplexer>() });
            }

            return services;
        }

        public static IServiceCollection AddWhaleDebug(this IServiceCollection services)
        {
#if DEBUG
            services.AddCors(o =>
            {
                o.AddPolicy("Allow dev ng", builder => builder.WithOrigins("http://localhost:4200"));
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v0", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Whalerator", Version = "v0" });
            });
#endif
            return services;
        }

        public static IServiceCollection AddWhaleDocuments(this IServiceCollection services, ConfigRoot config, PublicConfig uiConfig, ILogger logger)
        {
            bool contentWorker = config.ContentScanner != null;
            bool contentUI = (config.Search?.Filelists?.Count ?? 0) > 0;

            services.AddScoped<IAufsFilter, AufsFilter>();
            services.AddScoped<ILayerExtractor, LayerExtractor>();
            services.AddScoped<IDockerClient, LocalDockerClient>();

            if (contentWorker)
            {
                if (string.IsNullOrEmpty(config.ContentScanner?.LayerCache))
                {
                    logger.LogCritical("No layer cache specified, this worker will not be able to index content.");
                }
                services.AddHostedService<ContentScanWorker>();
            }

            if (contentUI)
            {
                uiConfig.DocScanner = true;
                uiConfig.SearchLists = config.Search?.Filelists?.Select(l => l.Split(';')?.Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)));
            }

            if (contentWorker || contentUI)
            {
                services.AddSingleton<IContentScanner, Content.ContentScanner>();
                if (string.IsNullOrEmpty(config.Cache.Redis))
                {
                    services.AddSingleton<IWorkQueue<Content.Request>, MemQueue<Content.Request>>();
                }
                else
                {
                    services.AddScoped<IWorkQueue<Content.Request>>(p => new RedQueue<Content.Request>(p.GetRequiredService<IConnectionMultiplexer>(),
                        p.GetRequiredService<ILogger<RedQueue<Content.Request>>>(),
                        Content.Request.WorkQueueKey));
                }
            }

            return services;
        }

        public static IServiceCollection AddWhaleVulnerabilities(this IServiceCollection services, ConfigRoot config, PublicConfig uiConfig)
        {
            bool clairWorker = config.ClairScanner != null;
            bool vulnUi = config.Search?.Vulnerabilities ?? false;

            uiConfig.SecScanner = vulnUi;

            if (clairWorker)
            {
                services.AddScoped(p => Refit.RestService.For<IClairAPI>(config.ClairScanner.ClairApi));
                services.AddHostedService<SecurityScanWorker>();
            }

            if (clairWorker || vulnUi)
            {
                services.AddSingleton<ISecurityScanner>(p => new ClairScanner(p.GetRequiredService<ILogger<ClairScanner>>(),
                    config,
                    p.GetService<IClairAPI>(),
                    p.GetRequiredService<ICacheFactory>(),
                    p.GetRequiredService<IWorkQueue<Security.Request>>()));
                if (string.IsNullOrEmpty(config.Cache.Redis))
                {
                    services.AddSingleton<IWorkQueue<Security.Request>, MemQueue<Security.Request>>();
                }
                else
                {
                    services.AddScoped<IWorkQueue<Security.Request>>(p => new RedQueue<Security.Request>(p.GetRequiredService<IConnectionMultiplexer>(),
                        p.GetRequiredService<ILogger<RedQueue<Security.Request>>>(),
                        Security.Request.WorkQueueKey));
                }
            }

            return services;
        }

    }
}
