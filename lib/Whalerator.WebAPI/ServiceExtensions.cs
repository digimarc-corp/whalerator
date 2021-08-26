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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Whalerator;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Queue;
using Whalerator.Security;
using Whalerator.Support;
using Whalerator.WebAPI.Workers;

namespace Whalerator.WebAPI
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddWhaleRegistry(this IServiceCollection services) => services.AddSingleton<IClientFactory, ClientFactory>();

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
                try
                {
                    crypto.ImportFromPem(File.ReadAllText(keyFile));
                    _ = crypto.ExportRSAPrivateKey();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"The supplied key ({keyFile}) does not contain a valid RSA private key.");
                    throw;
                }
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

            services.AddSingleton(p => p.GetService<ICacheFactory>().Get<Authorization>());
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
                services.AddSingleton<ICacheFactory>(provider => new MemCacheFactory(provider.GetService<IMemoryCache>()) { Ttl = IntervalParser.Parse(config.CacheTtl) });
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

                services.AddSingleton<ICacheFactory>(p => new RedCacheFactory { Mux = p.GetService<IConnectionMultiplexer>(), Ttl = IntervalParser.Parse(config.CacheTtl) });
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

        public static IServiceCollection AddWhalePublicConfig(this IServiceCollection services, ServiceConfig config)
        {
            if (config.Password.IsDefault)
            {
                throw new ArgumentException("Cannot set a default password.");
            }

            var docsEnabled = (config.Documents?.Count ?? 0) > 0;

            var uiConfig = new PublicConfig()
            {
                Themes = config.Themes,
                LoginBanner = Banners.ReadBanner(config.LoginBanner),
                DocScanner = docsEnabled,
                SearchLists = docsEnabled
                    ? config.Documents?.Select(l => l.Split(';')?.Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)))
                    : Array.Empty<string[]>(),
                SecScanner = config.Vulnerabilities,
                AutoLogin = config.AutoLogin,
                Registry = string.IsNullOrEmpty(config.Registry)
                    ? new() { PlaceholderText = "registry" }
                    : new() { PlaceholderText = config.Registry, IsDefault = true, IsReadonly = true },
                UserName = config.UserName,
                Password = config.Password
            };

            services.AddSingleton(uiConfig);
            return services;
        }

        public static IServiceCollection AddWhaleDocuments(this IServiceCollection services, ServiceConfig config, ILogger logger)
        {
            var contentWorker = config.IndexWorker;

            services.AddSingleton<IAufsFilter>(p => new AufsFilter() { CaseInsensitiveSearch = config.CaseInsensitive });
            services.AddSingleton<ILayerExtractor, LayerExtractor>();
            services.AddSingleton<IIndexStore>(p => new IndexStore() { StoreFolder = config.IndexFolder });

            if (contentWorker)
            {
                if (string.IsNullOrEmpty(config.RegistryCache) && string.IsNullOrEmpty(config.RegistryRoot))
                {
                    logger.LogCritical("No layer cache or local registry data specified, this worker will not be able to index content.");
                }
                services.AddHostedService<IndexWorker>();
            }

            if (string.IsNullOrEmpty(config.RedisCache))
            {
                services.AddSingleton<IWorkQueue<IndexRequest>, MemQueue<IndexRequest>>();
            }
            else
            {
                services.AddSingleton<IWorkQueue<IndexRequest>>(p => new RedQueue<IndexRequest>(p.GetRequiredService<IConnectionMultiplexer>(),
                    p.GetRequiredService<ILogger<RedQueue<IndexRequest>>>(),
                    IndexRequest.WorkQueueKey));
            }

            return services;
        }

        public static IServiceCollection AddWhaleVulnerabilities(this IServiceCollection services, ServiceConfig config, ILogger logger)
        {
            var clairWorker = config.ClairWorker;
            var vulnUi = config.Vulnerabilities;

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
                    p.GetRequiredService<IWorkQueue<ScanRequest>>()));
                if (string.IsNullOrEmpty(config.RedisCache))
                {
                    services.AddSingleton<IWorkQueue<ScanRequest>, MemQueue<ScanRequest>>();
                }
                else
                {
                    services.AddSingleton<IWorkQueue<ScanRequest>>(p => new RedQueue<ScanRequest>(p.GetRequiredService<IConnectionMultiplexer>(),
                        p.GetRequiredService<ILogger<RedQueue<ScanRequest>>>(),
                        ScanRequest.WorkQueueKey));
                }
            }

            return services;
        }


    }
}
