using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MobyInspector.Client
{
    public class MiniClient : IMiniClient
    {
        private ITokenSource _TokenSource;
        private Dictionary<string, AuthenticationHeaderValue> _Authorizations = new Dictionary<string, AuthenticationHeaderValue>();

        public TimeSpan Timeout { get; set; } = new TimeSpan(0, 3, 0);

        public MiniClient(ITokenSource tokenSource)
        {
            _TokenSource = tokenSource;
        }

        public HttpResponseMessage Get(Uri uri, string accept) => Get(uri, accept, retries: 3);


        private HttpResponseMessage Get(Uri uri, string accept, int retries)
        {
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = Timeout;
                HttpResponseMessage result;
                var message = new HttpRequestMessage { RequestUri = uri };
                if (_Authorizations.ContainsKey(uri.Host)) { message.Headers.Authorization = _Authorizations[uri.Host]; }
                if (!string.IsNullOrEmpty(accept)) { message.Headers.Add("Accept", accept); }

                result = client.SendAsync(message).Result;

                if (result.IsSuccessStatusCode)
                {
                    return result;
                }
                else if (retries > 0)
                {
                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        HandleUnauthorized(uri, result.Headers);
                        return Get(uri, accept, retries - 1);
                    }
                    else if (result.StatusCode == HttpStatusCode.Redirect || result.StatusCode == HttpStatusCode.RedirectKeepVerb)
                    {
                        //decriment retries even though nothing failed, to ensure we don't get caught in a redirect loop
                        return Get(result.Headers.Location, accept, retries - 1);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                        return Get(uri, accept, retries - 1);
                    }
                }
                else
                {
                    throw new Exception("Fuck");
                }
            }
        }
        private void HandleUnauthorized(Uri uri, HttpResponseHeaders headers)
        {
            var challenge = headers.WwwAuthenticate.First(h => h.Scheme == "Bearer");

            var token = _TokenSource.GetToken(challenge);
            _Authorizations[uri.Host] = AuthenticationHeaderValue.Parse($"Bearer {token}");
        }

    }
}
