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
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Security;

namespace Whalerator.WebAPI
{


    public class Program
    {
        public static void Main(string[] args)
        {
            Options options = null;
            var parseResult = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => options = o)
                .WithNotParsed(e => Environment.Exit(-1));

            if (!options.NoBanner)
            {
                PrintSplash();
            }

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    var configPath = Environment.GetEnvironmentVariable("CONFIGPATH");
                    if (string.IsNullOrEmpty(configPath))
                    {
                        configPath = Path.Combine(Environment.CurrentDirectory, "config.yaml");
                    }
                    else if (!File.Exists(configPath))
                    {
                        throw new ArgumentException($"The specified config file '{configPath}' could not be found.");
                    }

                    if (File.Exists(configPath))
                    {
                        try
                        {
                            config.AddYamlFile(configPath, optional: false, reloadOnChange: true);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"Could not load yaml-formatted configuration from the supplied file ({configPath}). Check the format of the file.\n{ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No configuration found, attempting to start with built-in defaults.");
                    }

                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    GetLogSettings(hostingContext, out var logLevel, out var msLogLevel, out var logStack, out var logHeader);

                    logging.SetMinimumLevel(logLevel);

                    logging.AddProvider(new ConsoleLoggerProvider(logStack, logHeader));
                    logging.AddFilter("Microsoft", (level) => level >= msLogLevel);
                    logging.AddDebug();
                });
            builder.UseStartup<Startup>();
            var webHost = builder.Build();

            if (options.Rescan)
            {
                Rescan(webHost.Services).Wait();
            }

            if (options.Exit)
            {
                Environment.Exit(0);
            }

            webHost.Run();
        }

        private static void GetLogSettings(WebHostBuilderContext hostingContext, out LogLevel logLevel, out LogLevel msLogLevel, out bool logStack, out bool logHeader)
        {
            try
            {
                var str = hostingContext.Configuration.GetValue(typeof(string), "logLevel") as string;
                logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), str, true);
            }
            catch
            {
                logLevel = LogLevel.Information;
                Console.WriteLine($"Could not get log level from config. Defaulting to '{logLevel}'");
            }

            try
            {
                var str = hostingContext.Configuration.GetValue(typeof(string), "msLogLevel") as string;
                msLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), str, true);
            }
            catch
            {
                msLogLevel = LogLevel.Warning;
            }

            try
            {
                logStack = (bool)hostingContext.Configuration.GetValue(typeof(bool), "logStack");
            }
            catch
            {
                logStack = false;
            }

            try
            {
                logHeader = (bool)hostingContext.Configuration.GetValue(typeof(bool), "logHeader");
            }
            catch
            {
                logHeader = false;
            }
        }

        private static void PrintSplash()
        {
            var color = Console.ForegroundColor;
            try
            {
                var banner = Figgle.FiggleFonts.Larry3d.Render("Whalerator");
                Console.ForegroundColor = ConsoleColor.Cyan;

                banner.Split('\n')
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList()
                    .ForEach(l => Console.WriteLine(l));
                Console.WriteLine("\n\t(c) 2018 Digimarc, Inc\n");
            }
            catch { }
            finally
            {
                Console.ForegroundColor = color;
            }
        }

        public static async Task Rescan(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            try
            {
                var processed = new HashSet<string>();
                var tags = 0;

                logger.LogInformation("Rescan requested.");
                var config = services.GetRequiredService<ServiceConfig>();
                if (string.IsNullOrEmpty(config.Registry))
                {
                    logger.LogError("A default registry must be configured to perform a rescan");
                }
                else
                {
                    var clientFactory = services.GetRequiredService<IClientFactory>();
                    var auth = services.GetRequiredService<IAuthHandler>();
                    await auth.LoginAsync(config.GetCatalogCredentials());
                    var client = clientFactory.GetClient(auth);

                    var indexQueue = services.GetRequiredService<Queue.IWorkQueue<IndexRequest>>();
                    var secScanner = services.GetRequiredService<Queue.IWorkQueue<ScanRequest>>();

                    foreach (var repo in client.GetRepositories())
                    {
                        try
                        {
                            foreach (var tag in client.GetTags(repo.Name))
                            {
                                tags++;
                                var digest = await client.GetTagDigestAsync(repo.Name, tag);
                                if (!processed.Contains(digest))
                                {
                                    processed.Add(digest);

                                    try
                                    {
                                        if (config.Documents?.Count > 0)
                                        {
                                            indexQueue.Push(new IndexRequest
                                            {
                                                CreatedTime = DateTime.UtcNow,
                                                TargetDigest = digest,
                                                TargetPaths = config.DeepIndexing ? null : config.Documents,
                                                TargetRepo = repo.Name
                                            });
                                            logger.LogInformation($"Queued index for {repo}:{tag}");
                                        }

                                        if (config.Vulnerabilities)
                                        {
                                            secScanner.Push(new Security.ScanRequest
                                            {
                                                CreatedTime = DateTime.UtcNow,
                                                TargetRepo = repo.Name,
                                                TargetDigest = digest
                                            });
                                            logger.LogInformation($"Queued scan for {repo}:{tag}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogError(ex, $"Error submitting {repo}:{tag}");
                                    }
                                }
                                else
                                {
                                    logger.LogInformation($"Duplicate digest {repo}:{tag}");
                                }
                            }
                        }
                        catch
                        {
                            logger.LogError($"Could not get tags for repository {repo}");
                        }
                    }
                }

                logger.LogInformation($"Submitted {processed.Count:N0} unique images from {tags:N0} tags for indexing and/or security scanning.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start rescan.");
            }
        }
    }
}
