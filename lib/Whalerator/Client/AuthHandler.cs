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
        private ICache<Authorization> _AuthCache;

        public AuthHandler(ICache<Authorization> cache)
        {
            _AuthCache = cache ?? new DictCache<Authorization>();
        }


        public string Username { get; private set; }
        public string Password { get; private set; }
        public string RegistryEndpoint { get; private set; }

        public string Service { get; private set; }
        public string Realm { get; private set; }

        /// <summary>
        /// If true, the registry is running in fully open mode, with no Authentication of any kind. Any calls to UpdateAuthorization will be ignored.
        /// </summary>
        public bool AnonymousMode { get; private set; }

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
            Password = password;
            RegistryEndpoint = Registry.HostToEndpoint(registryHost);

            var key = GetKey(null);

            if (_AuthCache.TryGet(key, out var authorization))
            {
                Service = authorization.Service;
                Realm = authorization.Realm;
            }
            else
            {
                using (var client = new HttpClient())
                {
                    var preLogin = client.GetAsync(RegistryEndpoint).Result;
                    if (preLogin.IsSuccessStatusCode)
                    {
                        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                        {
                            throw new AuthenticationException("A username or password was specified, but the registry server does not support authentication.");
                        }
                        else
                        {
                            AnonymousMode = true;
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
                                _AuthCache.Set(key, new Authorization { JWT = GetAuthorization(null).Parameter, Realm = Realm, Service = Service });
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
            return _AuthCache.TryGet(GetKey(scope), out var authorization) ? new AuthenticationHeaderValue("Bearer", authorization.JWT) : null;
        }

        public bool Authorize(string scope)
        {
            return HasAuthorization(scope) || UpdateAuthorization(scope);
        }

        public bool HasAuthorization(string scope)
        {
            scope = scope ?? string.Empty;
            return _AuthCache.Exists(GetKey(scope));
        }

        private string GetKey(string scope) => Authorization.CacheKey(RegistryEndpoint, Username, Password, scope);

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

        public bool UpdateAuthorization(string scope)
        {
            // registry is fully anonymous, so authorization is moot
            if (AnonymousMode) { return true; }

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
                    var access = JsonConvert.DeserializeAnonymousType(payload, new { access = new List<DockerAccess>() }).access;

                    if (string.IsNullOrEmpty(scope) || access.Any(a => a.Actions.Contains(action)))
                    {
                        _AuthCache.Set(GetKey(scope), new Authorization { JWT = token, Realm = Realm, Service = Service });
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
