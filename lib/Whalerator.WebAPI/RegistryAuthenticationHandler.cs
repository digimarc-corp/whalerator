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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class RegistryAuthenticationHandler : AuthenticationHandler<RegistryAuthenticationOptions>
    {
        public RegistryAuthenticationHandler(IOptionsMonitor<RegistryAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (string.IsNullOrEmpty(Request.Headers["Authorization"]))
            {
                return Task.FromResult(AuthenticateResult.Fail("{ \"error\": \"Authorization is required.\"}"));
            }
            else
            {
                try
                {
                    var header = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                    var token = Jose.JWT.Decode<Token>(header.Parameter, Options.Algorithm.ToDotNetRSA());

                    if (token.Exp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        return Task.FromResult(AuthenticateResult.Fail("{ \"error\": \"The token has expired\" }"));
                    }

                    var json = Options.Algorithm.Decrypt(token.Crd);
                    var credentials = JsonConvert.DeserializeObject<RegistryCredentials>(json);

                    if (!string.IsNullOrEmpty(Options.Registry) && Options.Registry.ToLowerInvariant() != credentials.Registry.ToLowerInvariant())
                    {
                        return Task.FromResult(AuthenticateResult.Fail("{ error: \"The supplied token is for an unsupported registry.\" }"));
                    }

                    var principal = new ClaimsPrincipal(credentials.ToClaimsIdentity());
                    var ticket = new AuthenticationTicket(principal, "token");

                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
                catch
                {
                    return Task.FromResult(AuthenticateResult.Fail("{ \"error\": \"The supplied token is invalid.\" }"));
                }
            }
        }
    }
}
