using MobyInspector.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MobyInspector.Client
{
    public class DistributionClient : IDistributionClient
    {
        private IAuthHandler _TokenSource;

        public TimeSpan Timeout { get; set; } = new TimeSpan(0, 3, 0);
        public string Host { get; set; } = "registry-1.docker.io";

        public DistributionClient(IAuthHandler tokenSource)
        {
            _TokenSource = tokenSource;
        }

        public Task<T> Get<T>(Uri uri, string accept = null)
        {
            var result = Get(uri, accept);
            var json = result.Content.ReadAsStringAsync().Result;
            return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
        }

        public HttpResponseMessage Get(Uri uri, string accept = null) => Get(uri, accept, retries: 3);
        
        private HttpResponseMessage Get(Uri uri, string accept, int retries)
        {
            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = Timeout;
                HttpResponseMessage result;
                var message = new HttpRequestMessage { RequestUri = uri };
                message.Headers.Authorization = _TokenSource.GetAuthorization(uri);

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
                        if (_TokenSource.HandleUnauthorized(uri, result.Headers))
                        {
                            return Get(uri, accept, retries - 1);
                        }
                        else
                        {
                            throw new Exception($"Access was denied to the remote resource, and authorization could not be obtained.");
                        }
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

        public Task<HttpResponseMessage> GetBlobAsync(string repository, string digest)
        {            
            return Task.FromResult(Get(new Uri($"https://{Host}/v2/{repository}/blobs/{digest}")));
        }

        public Task<RepositoryList> GetRepositoriesAsync()
        {            
            return Get<RepositoryList>(new Uri($"https://{Host}/v2/_catalog"));
        }

        public Task<TagList> GetTagsAsync(string repository)
        {
            return Get<TagList>(new Uri($"https://{Host}/v2/{repository}/tags/list"));
        }

        public Task<HttpResponseMessage> GetV2Manifest(string repository, string tag)
        {
            var uri = new Uri($"https://{Host}/v2/{repository}/manifests/{tag}");
            return Task.FromResult(Get(uri, "application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json"));
        }
    }
}
