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
            var crypto = new RSA(File.ReadAllText("key.pem"));

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = "Bearer";
                o.DefaultChallengeScheme = "Bearer";
                o.DefaultForbidScheme = "Bearer";
            }).AddScheme<RegistryAuthenticationOptions, RegistryAuthenticationHandler>("Bearer", o =>
            {
                o.Algorithm = crypto;
            });

            services.AddMvc().AddJsonOptions(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            });

            var redis = Configuration["redis"];
            if (string.IsNullOrEmpty(redis))
            {
                Logger.LogInformation("Using in-memory cache.");
                services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions { }));
                services.AddScoped<ICacheFactory>(provider => new MemCacheFactory(provider.GetService<IMemoryCache>(), new TimeSpan(0, 15, 0)));
            }
            else
            {
                Logger.LogInformation($"Using Redis cache ({redis})");
                var mux = ConnectionMultiplexer.Connect("localhost");
                services.AddScoped<ICacheFactory>(provider => new RedCacheFactory { Mux = mux, Db = 13, Ttl = new TimeSpan(0, 15, 0) });
            }
            services.AddScoped<ICache<Authorization>>(p => p.GetService<ICacheFactory>().Get<Authorization>());

            Logger.LogInformation($"Using layer cache ({Configuration["layercache"]})");
            services.AddScoped<IRegistryFactory>(provider => new RegistryFactory(provider.GetService<ICacheFactory>()) { LayerCache = Configuration["layercache"] });
            services.AddSingleton<ICryptoAlgorithm>(crypto);
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

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
