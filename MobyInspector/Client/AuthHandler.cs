using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace MobyInspector.Client
{
    public abstract class AuthHandler : IAuthHandler
    {
        protected Dictionary<string, AuthenticationHeaderValue> _Authorizations = new Dictionary<string, AuthenticationHeaderValue>();

        public virtual string GetToken(AuthenticationHeaderValue authenticateHeader)
        {
            var dict = Regex.Matches(authenticateHeader.Parameter, @"[\W]*(\w+)=""(.+?)""").Cast<Match>()
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);

            return GetToken(dict["realm"], dict["service"], dict["scope"]);
        }

        public abstract string GetToken(string realm, string service, string scope);

        // gets current auth info for a uri
        public virtual AuthenticationHeaderValue GetAuthorization(Uri uri)
        {
            return _Authorizations.ContainsKey(uri.Host) ? _Authorizations[uri.Host] : null;
        }

        // update auth for the given uri, and returns a value indicating success
        public virtual bool HandleUnauthorized(Uri uri, HttpResponseHeaders headers)
        {
            try
            {
                var challenge = headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

                var token = GetToken(challenge);
                _Authorizations[uri.Host] = AuthenticationHeaderValue.Parse($"Bearer {token}");
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
