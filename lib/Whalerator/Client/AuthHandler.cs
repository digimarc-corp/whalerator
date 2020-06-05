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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whalerator.Config;

namespace Whalerator.Client
{
    public class AuthHandler : IAuthHandler
    {
        private ICache<Authorization> authCache;
        private Dictionary<string, string> aliases;
        private readonly ServiceConfig config;
        private readonly ILogger<AuthHandler> logger;

        public AuthHandler(ICache<Authorization> cache, ServiceConfig config, ILogger<AuthHandler> logger)
        {
            authCache = cache;
            this.config = config;
            this.logger = logger;
            aliases = config.RegistryAliases.ToDictionary(a => a.Registry, a => a.Alias);
        }

        public string CatalogScope() => "registry:catalog:*";
        public string RepoPullScope(string repository) => $"repository:{repository}:pull";
        public string RepoPushScope(string repository) => $"repository:{repository}:push";
        public string RepoAdminScope(string repository) => $"repository:{repository}:*";

        public string GetRegistryEndpoint(bool ignoreAliases = false) =>
            RegistryCredentials.HostToEndpoint(GetRegistryHost(ignoreAliases));

        public string GetRegistryHost(bool ignoreAliases = false) =>
            !ignoreAliases && aliases.ContainsKey(RegistryCredentials.Registry) ? aliases[RegistryCredentials.Registry] : RegistryCredentials.Registry;

        string username => RegistryCredentials.Username;
        string password => RegistryCredentials.Password;

        public RegistryCredentials RegistryCredentials { get; private set; }

        /// <summary>
        /// If true, the current registry requires some form of token authentication
        /// </summary>
        public bool TokensRequired => DockerHub || !AnonymousMode;

        public string Service { get; private set; }
        public string Realm { get; private set; }

        public TimeSpan AuthTtl { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// If true, the registry is running in fully open mode, with no Authentication of any kind. Any calls to UpdateAuthorization will be ignored.
        /// </summary>
        public bool AnonymousMode { get; private set; }

        /// <summary>
        /// If true, the registry is running against Docker Hub, which has special cases attached
        /// </summary>
        public bool DockerHub { get; private set; }

        /// <summary>
        /// Initializes the set of authorizations for this auth handler, and validates credentials with the registry service.
        /// </summary>
        public Task LoginAsync(string registry, string username, string password) =>
            LoginAsync(new RegistryCredentials { Registry = registry, Username = username, Password = password });

        /// <summary>
        /// Initializes the set of authorizations for this auth handler, and validates credentials with the registry service.
        /// </summary>
        public async Task LoginAsync(RegistryCredentials credentials)
        {
            AnonymousMode = false;
            RegistryCredentials = credentials;

            var host = GetRegistryHost(config.IgnoreInternalAlias);
            var endpoint = GetRegistryEndpoint(config.IgnoreInternalAlias);

            var key = GetKey(null, granted: true);

            if (await authCache.ExistsAsync(key))
            {
                var authorization = await authCache.GetAsync(key);
                Service = authorization.Service;
                Realm = authorization.Realm;
                AnonymousMode = authorization.Anonymous;
                DockerHub = authorization.DockerHub;
            }
            else
            {
                using (var client = new HttpClient())
                {
                    AnonymousMode = (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password));

                    // if this is a docker hub client, we can't really validate until we have a real resource to fetch,
                    // but we can set some known values
                    if (host == RegistryCredentials.DockerHub)
                    {
                        DockerHub = true;
                        Realm = RegistryCredentials.DockerRealm;
                        Service = RegistryCredentials.DockerService;
                        return;
                    }

                    // first try an anonymous get for the registry catalog - if that succeeds but we're not in anonymous mode, throw
                    var preLogin = await client.GetAsync(endpoint + "/");
                    if (preLogin.IsSuccessStatusCode)
                    {
                        if (!AnonymousMode)
                        {
                            throw new AuthenticationException("A username or password was specified, but the registry server does not support authentication.");
                        }
                        else
                        {
                            // we're anon, and an anon catalog fetch worked, so we should be golden
                            return;
                        }
                    }
                    else if (preLogin.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        var challenge = ParseWwwAuthenticate(preLogin.Headers.WwwAuthenticate.First());
                        Service = challenge.service;
                        Realm = challenge.realm;

                        if (await UpdateAuthorizationAsync(null))
                        {
                            client.DefaultRequestHeaders.Authorization = await GetAuthorizationAsync(null);
                            var confirmation = client.GetAsync(endpoint + "/").Result;

                            if (!confirmation.IsSuccessStatusCode)
                            {
                                throw new AuthenticationException($"Authentication succeded against the realm '{Realm}', but the registry returned status code '{confirmation.StatusCode}'");
                            }
                            else
                            {
                                await authCache.SetAsync(key, new Authorization { JWT = (await GetAuthorizationAsync(null)).Parameter, Realm = Realm, Service = Service, Anonymous = AnonymousMode }, AuthTtl);
                            }
                        }
                        else
                        {
                            throw new AuthenticationException($"Authentication failed.");
                        }
                    }
                    else
                    {
                        throw new AuthenticationException($"The registry server returned an unexpected result code: {preLogin.StatusCode}");
                    }
                }
            }
        }

        public async Task<AuthenticationHeaderValue> GetAuthorizationAsync(string scope)
        {
            scope = scope ?? string.Empty;
            var key = GetKey(scope, granted: true);
            if (await authCache.ExistsAsync(key))
            {
                var authorization = await authCache.GetAsync(key);
                return new AuthenticationHeaderValue("Bearer", authorization.JWT);
            }
            else
            {
                return null;
            }
        }

        // DockerHub catalog is a special case, where we don't really have authorization, but we can create synthetic catalogs and need
        // psuedo-auth to allow caching etc
        public async Task<bool> AuthorizeAsync(string scope) => await HasAuthorizationAsync(scope) || await UpdateAuthorizationAsync(scope) || (DockerHub && scope == "registry:catalog:*");

        public async Task<bool> HasAuthorizationAsync(string scope)
        {
            scope = scope ?? string.Empty;
            return await authCache.ExistsAsync(GetKey(scope, granted: true));
        }

        private string GetKey(string scope, bool granted) => Authorization.CacheKey(GetRegistryEndpoint(), username, password, scope, granted);

        public string ParseScope(Uri uri)
        {
            if (TryParseScope(uri, out var scope))
            {
                return scope;
            }
            else
            {
                throw new ArgumentException($"Failed to parse {uri} as a valid Docker Registry scope.");
            }
        }

        public bool TryParseScope(Uri uri, out string scope)
        {
            scope = null;

            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts[0] == "v2")
            {
                if (parts.Length == 2 && parts[1] == "_catalog") { scope = "registry:catalog"; }
                else if (parts.Length >= 4)
                {
                    var repository = string.Join("/", parts.Skip(1).Take(parts.Length - 3));
                    scope = $"repository:{repository}";

                }
            }

            return scope != null;
        }

        public (string realm, string service, string scope) ParseWwwAuthenticate(AuthenticationHeaderValue header)
        {
            var dict = Regex.Matches(header.Parameter, @"[\W]*(\w+)=""(.+?)""").Cast<Match>()
               .ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);

            var realm = dict.Get("realm");
            var service = dict.Get("service");
            var scope = dict.Get("scope");

            return (realm, service, scope);
        }

        private string EncodeCredentials() => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        private string EncodeDefaultCredentials() => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.RegistryUser}:{config.RegistryPassword}"));


        /// <summary>
        /// Tries to get a new authorization for the requested scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="force">If true, ignore any cached denials and try the request again.</param>
        /// <returns></returns>
        public async Task<bool> UpdateAuthorizationAsync(string scope, bool force = false)
        {
            // registry is fully anonymous, so authorization is moot
            if (AnonymousMode && !DockerHub) { return true; }

            // if this user was recently denied for the same scope, don't waste a roundtrip on it
            if (!force && await authCache.ExistsAsync(GetKey(scope, granted: false))) { return false; }

            scope = scope ?? string.Empty;
            var action = string.IsNullOrEmpty(scope) ? null : scope.Split(':')[2].Split(',')[0];

            var token = await GetTokenAsync(scope);
            // if the call failed completely, return false but don't cache the result
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            // otherwise, process the claims returned
            else
            {
                var payload = Jose.JWT.Payload(token);
                var tokenObj = JsonConvert.DeserializeAnonymousType(payload, new { access = new List<DockerAccess>(), iat = UInt32.MinValue, exp = UInt32.MinValue });

                var expTime = DateTime.UnixEpoch.AddSeconds(tokenObj.exp).ToUniversalTime();

                var authorization = new Authorization { JWT = token, Realm = Realm, Service = Service };
                if (string.IsNullOrEmpty(scope) || tokenObj.access.Any(a => a.Actions.Contains(action)))
                {
                    await authCache.SetAsync(GetKey(scope, granted: true), authorization, expTime - DateTime.UtcNow);
                    return true;
                }
                else
                {
                    await authCache.SetAsync(GetKey(scope, granted: false), authorization, AuthTtl);
                    return false;
                }
            }
        }

        private async Task<string> GetTokenAsync(string scope)
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(Realm);

                var parameters = $"service={WebUtility.UrlEncode(Service)}";
                parameters += string.IsNullOrEmpty(scope) ? string.Empty : $"&scope={WebUtility.UrlEncode(scope)}";
                uri.Query = $"?{parameters}";

                // if we're getting catalog info, and we have a default user configured, use that instead
                if (scope == CatalogScope() && !string.IsNullOrEmpty(config.RegistryUser))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodeDefaultCredentials());
                }
                else if (!string.IsNullOrEmpty(username))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials());
                }
                var response = await client.GetAsync(uri.Uri);

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().Result, new { token = "" }).token;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
