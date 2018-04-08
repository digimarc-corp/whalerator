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
    public class AuthHandler : IAuthHandler2
    {
        Dictionary<(string service, string scope), AuthenticationHeaderValue> _Authorizations = new Dictionary<(string service, string scope), AuthenticationHeaderValue>();
        public string Username { get; set; }
        public string Password { get; set; }

        public AuthenticationHeaderValue GetAuthorization(string service, string scope)
        {
            return _Authorizations.Get((service, scope));
        }

        public bool HasAuthorization(string service, string scope)
        {
            return _Authorizations.ContainsKey((service, scope));
        }

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

        public bool UpdateAuthentication(string realm, string service, string scope)
        {
            var action = scope.Split(':')[2].Split(',')[0];
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(realm);

                var parameters = $"service={WebUtility.UrlEncode(service)}";
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

                    if (access.Any(a => a.Actions.Contains(action)))
                    {
                        _Authorizations[(service, scope)] = new AuthenticationHeaderValue("Bearer", token);
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
