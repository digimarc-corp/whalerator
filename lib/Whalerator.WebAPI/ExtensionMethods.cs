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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Whalerator.WebAPI
{
    public static class ExtensionMethods
    {
        public static IEnumerable<(string source, string pattern)> GetStaticDocuments(this IConfiguration configuration) => configuration.AsEnumerable()
            .Where(p => p.Key.StartsWith("staticDocs:", StringComparison.InvariantCultureIgnoreCase))
            .GroupBy(p => p.Key.Split(':')[1])
            .OrderBy(g => g.Key)
            .Select(g => (
                source: g.FirstOrDefault(k => k.Key.EndsWith("path", StringComparison.InvariantCultureIgnoreCase)).Value ?? g.FirstOrDefault(k => k.Key.EndsWith(g.Key)).Value,
                pattern: g.FirstOrDefault(k => k.Key.EndsWith("pattern", StringComparison.InvariantCultureIgnoreCase)).Value
            ));

        public static (LogLevel logLevel, LogLevel msLogLevel, bool logStack) GetLogSettings(this IConfiguration configuration)
        {
            LogLevel logLevel, msLogLevel;
            bool logStack;

            try
            {
                var str = configuration.GetValue(typeof(string), "logLevel") as string;
                logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), str, true);
            }
            catch
            {
                logLevel = LogLevel.Information;
                Console.WriteLine($"Could not get log level from config. Defaulting to '{logLevel}'");
            }

            try
            {
                var str = configuration.GetValue(typeof(string), "msLogLevel") as string;
                msLogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), str, true);
            }
            catch
            {
                msLogLevel = LogLevel.Warning;
            }

            try
            {
                logStack = (bool)configuration.GetValue(typeof(bool), "logStack");
            }
            catch
            {
                logStack = false;
            }

            return (logLevel, msLogLevel, logStack);
        }

        public static void LogEndpoints(this IApplicationBuilder app, ILogger log)
        {
            try
            {
                var addr = app.ServerFeatures[typeof(IServerAddressesFeature)] as IServerAddressesFeature;
                if (addr?.Addresses?.Count > 0)
                {
                    foreach (var a in addr?.Addresses)
                    {
                        log?.LogInformation($"Listening on {a}");
                    }
                }
                else
                {
                    log?.LogInformation("No listeners active.");
                }
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Couldn't get list of listening addresses.");
            }
        }

        public static RegistryCredentials ToRegistryCredentials(this ClaimsPrincipal principal)
        {
            return new RegistryCredentials
            {
                Registry = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Registry)))?.Value,
                Username = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Username)))?.Value,
                Password = principal?.Claims?.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Password)))?.Value
            };
        }

        public static ClaimsIdentity ToClaimsIdentity(this RegistryCredentials credentials)
        {
            var claims = new List<Claim>() { new Claim(nameof(RegistryCredentials.Registry), credentials.Registry, ClaimValueTypes.String) };
            if (!string.IsNullOrEmpty(credentials.Username)) { claims.Add(new Claim(nameof(RegistryCredentials.Username), credentials.Username, ClaimValueTypes.String)); }
            if (!string.IsNullOrEmpty(credentials.Password)) { claims.Add(new Claim(nameof(RegistryCredentials.Password), credentials.Password, ClaimValueTypes.String)); }

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }

    }
}
