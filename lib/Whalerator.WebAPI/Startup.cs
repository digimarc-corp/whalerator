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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Queue;
using Whalerator.Security;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            this.Logger = logger;
        }

        public IConfiguration Configuration { get; }
        public ILogger Logger { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigRoot();
            var uiConfig = new PublicConfig();
            Configuration.Bind(config);
            services.AddSingleton(config);
            services.AddSingleton(Logger);


            services.AddWhaleCrypto(config, Logger)
                .AddWhaleAuth()
                .AddWhaleDebug()
                .AddWhaleSerialization()
                .AddWhaleVulnerabilities(config, uiConfig)
                .AddWhaleDocuments(config, uiConfig, Logger)
                .AddWhaleCache(config, Logger)
                .AddWhaleRegistry(config, uiConfig, Logger);

            services.AddSingleton(uiConfig);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v0/swagger.json", "Whalerator v0");
            });
#endif

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // serve angular SPA
            var options = new RewriteOptions()
                .AddRewrite("^login.*", "index.html", skipRemainingRules: true)
                .AddRewrite("^catalog.*", "index.html", skipRemainingRules: true)
                .AddRewrite(@"^r\/.*", "index.html", skipRemainingRules: true);
            app.UseRewriter(options);
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
