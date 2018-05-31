using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/config")]
    public class ConfigController : Controller
    {
        public ConfigController(ILogger<ConfigController> logger, Config config)
        {
            Logger = logger;
            Config = config;
        }

        public ILogger<ConfigController> Logger { get; }
        public Config Config { get; }

        /// <summary>
        /// Returns configuration options for the Whalerator UI SPA
        /// </summary>
        /// <returns></returns>
        public IActionResult Get()
        {
            return Ok(new
            {
                Registry = Config.Catalog?.Registry,
                SearchLists = Config.Search?.Filelists?.Select(l => l.Split(';')?.Select(f => f.Trim()))
            });
        }
    }
}