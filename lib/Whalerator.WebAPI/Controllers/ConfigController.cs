﻿/*
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Whalerator.Config;

namespace Whalerator.WebAPI.Controllers
{
    [Route(Startup.ApiBase + "[controller]")]
    public class ConfigController : ControllerBase
    {
        public ConfigController(ILogger<ConfigController> logger, PublicConfig config)
        {
            Logger = logger;
            Config = config;
        }

        public ILogger<ConfigController> Logger { get; }
        public PublicConfig Config { get; }

        /// <summary>
        /// Returns configuration options for the Whalerator UI SPA
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get() => Ok(Config);
    }
}