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
                    if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                    {
                        configPath = Path.Combine(Environment.CurrentDirectory, "config.yaml");
                    }

                    if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
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
