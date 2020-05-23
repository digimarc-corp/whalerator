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

using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Whalerator.Config;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class RegistryAuthenticationDecoder
    {
        private ICryptoAlgorithm crypto;
        private ServiceConfig config;

        public RegistryAuthenticationDecoder(ICryptoAlgorithm crypto, ServiceConfig config)
        {
            this.crypto = crypto;
            this.config = config;
        }

        public Task<AuthenticateResult> AuthenticateAsync(string authorization)
        {
            try
            {
                var header = AuthenticationHeaderValue.Parse(authorization);
                var token = Jose.JWT.Decode<Token>(header.Parameter, crypto.ToDotNetRSA());

                if (token.Exp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return Task.FromResult(AuthenticateResult.Fail("{ \"error\": \"The token has expired\" }"));
                }

                var json = crypto.Decrypt(token.Crd);
                var credentials = JsonConvert.DeserializeObject<RegistryCredentials>(json);

                if (!string.IsNullOrEmpty(config.Registry) && ClientFactory.DeAliasDockerHub(config.Registry.ToLowerInvariant()) != credentials.Registry.ToLowerInvariant())
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
