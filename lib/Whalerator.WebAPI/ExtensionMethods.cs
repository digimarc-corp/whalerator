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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        public static IEnumerable<(string source, string pattern)> GetStaticDocuments(this IConfiguration configuration) => configuration.AsEnumerable()
            .Where(p => p.Key.StartsWith("staticDocs:", StringComparison.InvariantCultureIgnoreCase))
            .GroupBy(p => p.Key.Split(':')[1])
            .OrderBy(g => g.Key)
            .Select(g => (
                source: g.FirstOrDefault(k => k.Key.EndsWith("path", StringComparison.InvariantCultureIgnoreCase)).Value ?? g.FirstOrDefault(k => k.Key.EndsWith(g.Key)).Value,
                pattern: g.FirstOrDefault(k => k.Key.EndsWith("pattern", StringComparison.InvariantCultureIgnoreCase)).Value
            ));


        public static RegistryCredentials ToRegistryCredentials(this ClaimsPrincipal principal)
        {
            return new RegistryCredentials
            {
                Registry = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Registry)))?.Value,
                Username = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Username)))?.Value,
                Password = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Password)))?.Value
            };
        }

        public static ClaimsIdentity ToClaimsIdentity(this RegistryCredentials credentials)
        {
            var claims = new List<Claim>() { new Claim(nameof(RegistryCredentials.Registry), credentials.Registry, ClaimValueTypes.String) };
            if (!string.IsNullOrEmpty(credentials.Username)) { claims.Add(new Claim(nameof(RegistryCredentials.Username), credentials.Username, ClaimValueTypes.String)); }
            if (!string.IsNullOrEmpty(credentials.Password)) { claims.Add(new Claim(nameof(RegistryCredentials.Password), credentials.Password, ClaimValueTypes.String)); }

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }

        public static IServiceCollection AddWhaleRegistry(this IServiceCollection services, ServiceConfig config, PublicConfig uiConfig)
        {
            services.AddScoped<IClientFactory, ClientFactory>();

            uiConfig.Registry = config.Registry;
            uiConfig.AutoLogin = config.AutoLogin;

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

        public static IServiceCollection AddWhaleCrypto(this IServiceCollection services, ServiceConfig config, ILogger logger)
        {
            System.Security.Cryptography.RSA crypto;

            var keyFile = config.AuthTokenKey;
            if (!string.IsNullOrEmpty(keyFile) && File.Exists(keyFile))
            {
                logger?.LogInformation($"Loading private key from {config.AuthTokenKey}.");
                crypto = System.Security.Cryptography.RSA.Create();
                crypto.ImportFromPem(File.ReadAllText(keyFile));
            }
            else
            {
                logger?.LogInformation($"Generating temporary private key.");
                crypto = System.Security.Cryptography.RSA.Create(4096);
            }
            services.AddSingleton<System.Security.Cryptography.AsymmetricAlgorithm>(crypto);

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

        public static IServiceCollection AddWhaleCache(this IServiceCollection services, ServiceConfig config, ILogger logger)
        {
            logger?.LogInformation($"Default object cache lifetime: {config.CacheTtl}");

            if (string.IsNullOrEmpty(config.RedisCache))
            {
                logger?.LogInformation("Using in-memory cache.");
                services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions { }));
                services.AddScoped<ICacheFactory>(provider => new MemCacheFactory(provider.GetService<IMemoryCache>()) { Ttl = IntervalParser.Parse(config.CacheTtl) });
            }
            else
            {
                logger?.LogInformation($"Using Redis cache ({config.RedisCache})");
                var ready = false;
                var retryTime = 15;
                while (!ready)
                {
                    try
                    {
                        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(config.RedisCache));
                        ready = true;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning($"Could not connect to redis instance. Retrying in {retryTime}s.");
                        logger?.LogInformation($"Redis connection string: {config.RedisCache}");
                        logger?.LogError(ex, "Redis connection error");
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(retryTime));
                    }
                }

                services.AddScoped<ICacheFactory>(p => new RedCacheFactory { Mux = p.GetService<IConnectionMultiplexer>(), Ttl = IntervalParser.Parse(config.CacheTtl) });
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
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Whalerator", Version = "v1" });
            });
#endif
            return services;
        }

        public static IServiceCollection AddWhaleDocuments(this IServiceCollection services, ServiceConfig config, PublicConfig uiConfig, ILogger logger)
        {
            bool contentWorker = config.IndexWorker;
            bool contentUI = (config.Documents?.Count ?? 0) > 0;

            services.AddScoped<IAufsFilter>(p => new AufsFilter() { CaseInsensitiveSearch = config.CaseInsensitive });
            services.AddScoped<ILayerExtractor, LayerExtractor>();

            services.AddScoped<IIndexStore>(p => new IndexStore() { StoreFolder = config.IndexFolder });

            if (contentWorker)
            {
                if (string.IsNullOrEmpty(config.RegistryCache) && string.IsNullOrEmpty(config.RegistryRoot))
                {
                    logger.LogCritical("No layer cache or local registry data specified, this worker will not be able to index content.");
                }
                services.AddHostedService<IndexWorker>();
            }

            if (contentUI)
            {
                uiConfig.DocScanner = true;
                uiConfig.SearchLists = config.Documents?.Select(l => l.Split(';')?.Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)));
            }

            if (string.IsNullOrEmpty(config.RedisCache))
            {
                services.AddSingleton<IWorkQueue<IndexRequest>, MemQueue<IndexRequest>>();
            }
            else
            {
                services.AddScoped<IWorkQueue<IndexRequest>>(p => new RedQueue<IndexRequest>(p.GetRequiredService<IConnectionMultiplexer>(),
                    p.GetRequiredService<ILogger<RedQueue<IndexRequest>>>(),
                    IndexRequest.WorkQueueKey));
            }

            return services;
        }

        public static IServiceCollection AddWhaleVulnerabilities(this IServiceCollection services, ServiceConfig config, PublicConfig uiConfig, ILogger logger)
        {
            bool clairWorker = config.ClairWorker;
            bool vulnUi = config.Vulnerabilities;

            uiConfig.SecScanner = vulnUi;

            if (clairWorker)
            {
                if (string.IsNullOrEmpty(config.ClairApi))
                {
                    logger.LogCritical("ClairApi is not configured, cannot start security worker.");
                }
                else
                {
                    services.AddScoped(p => Refit.RestService.For<IClairAPI>(config.ClairApi));
                    services.AddHostedService<SecurityScanWorker>();
                }
            }

            if (clairWorker || vulnUi)
            {
                services.AddSingleton<ISecurityScanner>(p => new ClairScanner(p.GetRequiredService<ILogger<ClairScanner>>(),
                    config,
                    p.GetService<IClairAPI>(),
                    p.GetRequiredService<ICacheFactory>(),
                    p.GetRequiredService<IWorkQueue<Security.ScanRequest>>()));
                if (string.IsNullOrEmpty(config.RedisCache))
                {
                    services.AddSingleton<IWorkQueue<Security.ScanRequest>, MemQueue<Security.ScanRequest>>();
                }
                else
                {
                    services.AddScoped<IWorkQueue<Security.ScanRequest>>(p => new RedQueue<Security.ScanRequest>(p.GetRequiredService<IConnectionMultiplexer>(),
                        p.GetRequiredService<ILogger<RedQueue<Security.ScanRequest>>>(),
                        Security.ScanRequest.WorkQueueKey));
                }
            }

            return services;
        }

    }
}
