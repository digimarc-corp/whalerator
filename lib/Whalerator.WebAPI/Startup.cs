using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Whalerator.Client;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            services.AddScoped<ICache<Authorization>>((provider) =>
            {
                return new MemCache<Authorization>(provider.GetService<IMemoryCache>(), new TimeSpan(1, 0, 0));
            });
            services.AddScoped<IRegistryFactory, RegistryFactory>();
            services.AddScoped<ICache<IEnumerable<string>>>((provider) =>
            {
                return new MemCache<IEnumerable<string>>(provider.GetService<IMemoryCache>(), new TimeSpan(0, 15, 0));
            });
            services.AddScoped<ICacheFactory>((provider) =>
            {
                return new MemCacheFactory(provider.GetService<IMemoryCache>(), new TimeSpan(0, 15, 0));
            });
            services.AddSingleton<ICryptoAlgorithm>(crypto);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
