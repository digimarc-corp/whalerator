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
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jose;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route(Startup.ApiBase + "[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly AsymmetricAlgorithm crypto;
        private readonly ICache<Authorization> cache;
        private readonly ILoggerFactory loggerFactory;

        public ILogger<TokenController> Logger { get; }
        public ServiceConfig Config { get; }

        public TokenController(AsymmetricAlgorithm crypto, ICache<Authorization> cache, ILoggerFactory loggerFactory, ServiceConfig config)
        {
            this.crypto = crypto;
            this.cache = cache;
            this.loggerFactory = loggerFactory;
            Logger = this.loggerFactory.CreateLogger<TokenController>();
            Config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] RegistryCredentials credentials)
        {
            // must specify a registry
            if (string.IsNullOrEmpty(credentials.Registry)) { return Unauthorized(); }

            // deny requests for foreign instances, if configured
            if (!string.IsNullOrEmpty(Config.Registry) && credentials.Registry.ToLowerInvariant() != Config.Registry.ToLowerInvariant())
            {
                return Unauthorized();
            }
            try
            {
                credentials.Registry = RegistryCredentials.DeAliasDockerHub(credentials.Registry);
                var handler = new AuthHandler(cache, Config, loggerFactory.CreateLogger<AuthHandler>());
                await handler.LoginAsync(credentials.Registry, credentials.Username, credentials.Password);
                var json = JsonConvert.SerializeObject(credentials);

                // publicly visible parameters for session validation
                var headers = new Dictionary<string, object>
                {
                    { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                    { "exp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.AuthTokenLifetime },
                    { "reg", credentials.Registry }
                };

                var token = new Token
                {
                    Usr = credentials.Username,
                    Pwd = credentials.Password,
                    Reg = credentials.Registry,
                    Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.AuthTokenLifetime
                };

                var jwe = Jose.JWT.Encode(token, crypto, JweAlgorithm.RSA_OAEP, JweEncryption.A256GCM, extraHeaders: headers);

                return Ok(new
                {
                    token = jwe
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error authenticating token request.");
                return Unauthorized();
            }
        }
    }
}
