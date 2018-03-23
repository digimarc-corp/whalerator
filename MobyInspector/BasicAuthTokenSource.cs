using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace MobyInspector
{
    public class BasicAuthTokenSource: TokenSourceBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        private string EncodeCredentials()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Password}"));
        }

        public override string GetToken(string realm, string service, string scope)
        {
            using (var client = new HttpClient())
            {

                var uri = new UriBuilder(realm);
                uri.Query = $"?service={WebUtility.UrlEncode(service)}&scope={WebUtility.UrlEncode(scope)}";
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", EncodeCredentials());

                var response = client.GetAsync(uri.Uri).Result;

                var obj = JsonConvert.DeserializeAnonymousType(response.Content.ReadAsStringAsync().Result, new { token = "" });

                return obj.token;
            }
        }

    }
}
