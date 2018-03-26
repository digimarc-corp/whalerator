using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace MobyInspector
{
    public abstract class TokenSource : ITokenSource
    {
        public string GetToken(AuthenticationHeaderValue authenticateHeader)
        {
            var dict = Regex.Matches(authenticateHeader.Parameter, @"[\W]*(\w+)=""(.+?)""").Cast<Match>()
                .ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);

            return GetToken(dict["realm"], dict["service"], dict["scope"]);
        }

        public abstract string GetToken(string realm, string service, string scope);
    }
}
