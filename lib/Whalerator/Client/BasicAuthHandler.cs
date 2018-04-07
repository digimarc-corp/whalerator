using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Linq;

namespace Whalerator.Client
{
    public class BasicAuthHandler : AuthHandler
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        private string EncodeCredentials()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}"));
        }

        public override string GetToken(string realm, params (string name, string value)[] values)
        {
            using (var client = new HttpClient())
            {
                var uri = new UriBuilder(realm);

                var parameters = string.Join("&", values.Select(v => $"{v.name}={WebUtility.UrlEncode(v.value)}"));
                uri.Query = $"?{parameters}";

                var response = client.GetAsync(uri.Uri).Result;
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", EncodeCredentials());
                    response = client.GetAsync(uri.Uri).Result;
                }

                var obj = JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().Result, new { token = "" });

                return obj.token;
            }
        }

        public override string GetCachePrefix() => UserName;
    }
}
