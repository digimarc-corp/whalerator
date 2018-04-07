using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Whalerator.Client
{
    public class AnonAuthHandler : AuthHandler
    {
        public override string GetCachePrefix() => "anon";

        public override string GetToken(string realm, params (string name, string value)[] values)
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(realm);

                var parameters = string.Join("&", values.Select(v => $"{v.name}={WebUtility.UrlEncode(v.value)}"));
                uri.Query = $"?{parameters}";

                var response = client.GetAsync(uri.Uri).Result;

                var obj = JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().Result, new { token = "" });

                return obj.token;
            }
        }
    }
}
