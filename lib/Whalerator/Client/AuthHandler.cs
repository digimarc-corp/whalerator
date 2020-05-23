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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Whalerator.Client
{
    public class AuthHandler : IAuthHandler
    {
        private ICache<Authorization> authCache;

        public AuthHandler(ICache<Authorization> cache)
        {
            authCache = cache;
        }

        public string CatalogScope() => "registry:catalog:*";
        public string RepoPullScope(string repository) => $"repository:{repository}:pull";
        public string RepoPushScope(string repository) => $"repository:{repository}:push";
        public string RepoAdminScope(string repository) => $"repository:{repository}:*";


        public string Username { get; private set; }
        public string Password { get; private set; }
        public string RegistryEndpoint { get; private set; }
        public string RegistryHost { get; private set; }

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
        /// <param name="registryHost">Actual registry host, i.e. myregistry.io</param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void Login(string registryHost, string username = null, string password = null)
        {
            AnonymousMode = false;
            Username = username;
            Password = password ?? string.Empty;
            RegistryHost = registryHost;
            RegistryEndpoint = ClientFactory.HostToEndpoint(registryHost);

            var key = GetKey(null, granted: true);

            if (authCache.TryGet(key, out var authorization))
            {
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
                    if (registryHost == ClientFactory.DockerHub)
                    {
                        DockerHub = true;
                        Realm = ClientFactory.DockerRealm;
                        Service = ClientFactory.DockerService;
                        return;
                    }

                    // first try an anonymous get for the registry catalog - if that succeeds but we're in anonymous mode, throw
                    var preLogin = client.GetAsync(RegistryEndpoint).Result;
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

                        if (UpdateAuthorization(null))
                        {
                            client.DefaultRequestHeaders.Authorization = GetAuthorization(null);
                            var confirmation = client.GetAsync(RegistryEndpoint).Result;

                            if (!confirmation.IsSuccessStatusCode)
                            {
                                throw new AuthenticationException($"Authentication succeded against the realm '{Realm}', but the registry returned status code '{confirmation.StatusCode}'");
                            }
                            else
                            {
                                authCache.Set(key, new Authorization { JWT = GetAuthorization(null).Parameter, Realm = Realm, Service = Service, Anonymous = AnonymousMode }, AuthTtl);
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

        public AuthenticationHeaderValue GetAuthorization(string scope)
        {
            scope = scope ?? string.Empty;
            return authCache.TryGet(GetKey(scope, granted: true), out var authorization) ? new AuthenticationHeaderValue("Bearer", authorization.JWT) : null;
        }

        public bool Authorize(string scope)
        {
            return HasAuthorization(scope) || UpdateAuthorization(scope) || (DockerHub && scope == "registry:catalog:*");
        }

        public bool HasAuthorization(string scope)
        {
            scope = scope ?? string.Empty;
            return authCache.Exists(GetKey(scope, granted: true));
        }

        private string GetKey(string scope, bool granted) => Authorization.CacheKey(RegistryEndpoint, Username, Password, scope, granted);

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

        private string EncodeCredentials()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        }

        /// <summary>
        /// Tries to get a new authorization for the requested scope.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="force">If true, ignore any cached denials and try the request again.</param>
        /// <returns></returns>
        public bool UpdateAuthorization(string scope, bool force = false)
        {
            // registry is fully anonymous, so authorization is moot
            if (AnonymousMode && !DockerHub) { return true; }

            // if this user was recently denied for the same scope, don't waste a roundtrip on it
            if (!force && authCache.Exists(GetKey(scope, granted: false))) { return false; }

            scope = scope ?? string.Empty;
            var action = string.IsNullOrEmpty(scope) ? null : scope.Split(':')[2].Split(',')[0];
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(Realm);

                var parameters = $"service={WebUtility.UrlEncode(Service)}";
                parameters += string.IsNullOrEmpty(scope) ? string.Empty : $"&scope={WebUtility.UrlEncode(scope)}";
                uri.Query = $"?{parameters}";

                if (!string.IsNullOrEmpty(Username))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials());
                }
                var response = client.GetAsync(uri.Uri).Result;

                if (response.IsSuccessStatusCode)
                {
                    var token = JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().Result, new { token = "" }).token;
                    var payload = Jose.JWT.Payload(token);
                    var tokenObj = JsonConvert.DeserializeAnonymousType(payload, new { access = new List<DockerAccess>(), iat = UInt32.MinValue, exp = UInt32.MinValue });

                    var expTime = DateTime.UnixEpoch.AddSeconds(tokenObj.exp).ToUniversalTime();

                    var authorization = new Authorization { JWT = token, Realm = Realm, Service = Service };
                    if (string.IsNullOrEmpty(scope) || tokenObj.access.Any(a => a.Actions.Contains(action)))
                    {
                        authCache.Set(GetKey(scope, granted: true), authorization, expTime - DateTime.UtcNow);
                        return true;
                    }
                    else
                    {
                        authCache.Set(GetKey(scope, granted: false), authorization, AuthTtl);
                    }
                }
                return false;
            }
        }
    }
}
