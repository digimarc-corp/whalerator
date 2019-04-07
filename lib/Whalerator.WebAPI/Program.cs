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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Whalerator.WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = new WebHostBuilder()
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
                        throw new ArgumentException($"The specified config file '{configPath}' couuld not be found.");
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
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseStartup<Startup>()
                .Build();

            webHost.Run();
        }
    }
}
