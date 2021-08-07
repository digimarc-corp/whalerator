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

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class RegistryAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly RegistryAuthenticationDecoder decoder;

        public RegistryAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, RegistryAuthenticationDecoder decoder)
            : base(options, logger, encoder, clock)
        {
            this.decoder = decoder;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string auth;
            if (string.IsNullOrEmpty(Request.Headers["Authorization"]))
            {
                if (Request.Cookies.ContainsKey("jwt"))
                {
                    var token = Request.Cookies["jwt"];
                    auth = $"Bearer {token}";
                }
                else
                {
                    return Task.FromResult(AuthenticateResult.Fail("{ \"error\": \"Authorization is required.\"}"));
                }
            }
            else
            {
                auth = Request.Headers["Authorization"];
            }
            return decoder.AuthenticateAsync(auth);
        }

    }
}
