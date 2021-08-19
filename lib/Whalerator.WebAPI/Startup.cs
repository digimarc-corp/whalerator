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
using System.Text;
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
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var (logLevel, msLogLevel, logStack) = Configuration.GetLogSettings();
            logger = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(logLevel)
                .AddConsoleFormatter<AnsiLogFormatter, AnsiLogOptions>(o => o.LogStack = logStack)
                .AddConsole(o => o.FormatterName = nameof(AnsiLogFormatter)))
                .CreateLogger("Startup");

            Config = new ServiceConfig();
            Configuration.Bind(Config);

            // load and cache static documents
            Config.StaticDocuments = Configuration.GetStaticDocuments().ToList();

        }

        public const string ApiBase = "api/v1/";

        public IConfiguration Configuration { get; }
        public ServiceConfig Config { get; }
        private readonly ILogger logger;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddSingleton(Config);
                services.AddSingleton(logger);

                services.AddWhaleCrypto(Config, logger)
                    .AddWhaleAuth()
                    .AddWhaleDebug()
                    .AddWhaleSerialization()
                    .AddWhaleVulnerabilities(Config, logger)
                    .AddWhaleDocuments(Config, logger)
                    .AddWhaleCache(Config, logger)
                    .AddWhaleRegistry()
                    .AddWhalePublicConfig(Config);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "There was an error during startup.");
                Environment.Exit(-1);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // remove any leading/trailing slashes so we can format them correctly below
            var baseUrl = Config.BaseUrl?.Trim('/') ?? string.Empty;
            if (!string.IsNullOrEmpty(baseUrl)) { app.UsePathBase($"/{baseUrl}"); }

            //reformat repository requests to allow paths like /api/repository/some/arbitrary/path/tags
            app.UseActionReverser($"/{ApiBase}repository", 2);

            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            );

#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Whalerator v1");
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
                .AddRewrite($"^login.*", "index.html", skipRemainingRules: true)
                .AddRewrite($"^catalog.*", "index.html", skipRemainingRules: true)
                .AddRewrite($"^r\\/.*", "index.html", skipRemainingRules: true);
            app.UseRewriter(options);
            app.UseDefaultFiles();

            try
            {
                // if we're configured with a custom baseUrl, rewrite the SPA index.html
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    var index = File.ReadAllText("wwwroot/index.html");
                    var regex = new System.Text.RegularExpressions.Regex("<base (.*)>");
                    var newIndex = regex.Replace(index, $"<base href=\"/{baseUrl}/\">");

                    app.Map("/index.html", (c) => c.Run((context) =>
                    {
                        context.Response.ContentType = "text/html";
                        context.Response.WriteAsync(newIndex);
                        return Task.FromResult(new Microsoft.AspNetCore.Mvc.OkResult());
                    }));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not rewrite SPA baseUrl to reflect custom value '{baseUrl}'");
            }

            app.UseStaticFiles();

            app.LogEndpoints(logger);
        }
    }
}
