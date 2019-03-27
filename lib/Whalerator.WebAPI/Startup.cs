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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Scanner;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            Logger = logger;
        }

        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigRoot();
            Configuration.Bind(config);
            services.AddSingleton(config);

            services.AddSingleton(Logger);

            RSA crypto;
            var keyFile = config.Security?.PrivateKey;
            if (!string.IsNullOrEmpty(keyFile) && File.Exists(keyFile))
            {
                Logger?.LogInformation($"Loading private key from {config.Security.PrivateKey}.");
                crypto = new RSA(File.ReadAllText("key.pem"));
            }
            else
            {
                Logger?.LogInformation($"Generating temporary private key.");
                crypto = new RSA(2048);
            }
            services.AddSingleton<ICryptoAlgorithm>(crypto);

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Bearer";
                o.DefaultChallengeScheme = "Bearer";
                o.DefaultForbidScheme = "Bearer";
            }).AddScheme<RegistryAuthenticationOptions, RegistryAuthenticationHandler>("Bearer", o =>
            {
                o.Algorithm = crypto;
                o.Registry = config.Catalog?.Registry;
            });

            services.AddCors();

            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            var staticTtl = config.Cache.StaticTtl == 0 ? (TimeSpan?)null : new TimeSpan(0, 0, config.Cache.StaticTtl);
            var volatileTtl = new TimeSpan(0, 0, config.Cache.VolatileTtl);

            if (string.IsNullOrEmpty(config.Cache.Redis))
            {
                Logger?.LogInformation("Using in-memory cache.");
                services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions { }));
                services.AddScoped<ICacheFactory>(provider => new MemCacheFactory(provider.GetService<IMemoryCache>(), volatileTtl));
            }
            else
            {
                Logger.LogInformation($"Using Redis cache ({config.Cache.Redis})");
                var mux = ConnectionMultiplexer.Connect(config.Cache.Redis);
                services.AddScoped<ICacheFactory>(provider => new RedCacheFactory { Mux = mux, Db = 13, Ttl = volatileTtl });
            }
            services.AddScoped(p => p.GetService<ICacheFactory>().Get<Authorization>());
            services.AddTransient<IAuthHandler, AuthHandler>();
            services.AddScoped<IDistributionClient, DistributionClient>();

            Logger?.LogInformation($"Cache lifetime for volatile objects: {volatileTtl}");
            Logger?.LogInformation($"Cache lifetime for static objects: {(staticTtl == null ? "unlimited" : staticTtl.ToString())}");
            Logger.LogInformation($"Using layer cache ({config.Cache.LayerCache})");

            if (!string.IsNullOrEmpty(config.Clair?.ClairApi))
            {
                services.AddSingleton<ISecurityScanner, ClairScanner>();
            }

            services.AddScoped<IRegistryFactory>(p =>
            {
                var catalogHandler = string.IsNullOrEmpty(config.Catalog?.User?.Username) ? null : p.GetService<IAuthHandler>();
                catalogHandler?.Login(config.Catalog.Registry, config.Catalog.User.Username, config.Catalog.User.Password);

                var settings = new RegistrySettings
                {
                    AuthHandler = p.GetService<IAuthHandler>(),
                    CacheFactory = p.GetService<ICacheFactory>(),
                    CatalogAuthHandler = catalogHandler,
                    HiddenRepos = config.Catalog?.Hidden,
                    LayerCache = config.Cache.LayerCache,
                    StaticRepos = config.Catalog?.Repositories,
                    StaticTtl = staticTtl,
                    VolatileTtl = volatileTtl
                };
                return new RegistryFactory(settings);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //reformat repository requests to allow paths like /api/repository/some/arbitrary/path/tags
            app.UseActionReverser("/api/repository", 2);

            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            );

            app.UseAuthentication();
            app.UseMvc();

            // serve angular SPA
            var options = new RewriteOptions()
                .AddRewrite("^login.*", "index.html", skipRemainingRules: true)
                .AddRewrite("^catalog.*", "index.html", skipRemainingRules: true)
                .AddRewrite("^repo.*", "index.html", skipRemainingRules: true);
            app.UseRewriter(options);
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
