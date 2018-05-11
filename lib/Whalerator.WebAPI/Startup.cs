using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using Whalerator.Client;
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
            var config = new Config();
            Configuration.Bind(config);

            RSA crypto;
            if (File.Exists(config.Security.PrivateKey))
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
            Logger?.LogInformation($"Cache lifetime for volatile objects: {volatileTtl}");
            Logger?.LogInformation($"Cache lifetime for static objects: {(staticTtl == null ? "unlimited" : staticTtl.ToString())}");
            services.AddScoped(p => p.GetService<ICacheFactory>().Get<Authorization>());
            Logger.LogInformation($"Using layer cache ({config.Cache.LayerCache})");
            services.AddScoped<IRegistryFactory>(provider => new RegistryFactory(provider.GetService<ICacheFactory>()) { LayerCache = config.Cache.LayerCache, VolatileTtl = volatileTtl, StaticTtl = staticTtl });
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
        }
    }
}
