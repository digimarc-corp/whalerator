using Whalerator.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Whalerator.Client
{
    public class DistributionClient : IDistributionClient
    {
        private IAuthHandler _TokenSource;
        private ICacheFactory _CacheFactory;

        public TimeSpan Timeout { get; set; } = new TimeSpan(0, 3, 0);
        public string Host { get; set; } = Registry.DockerHub;

        public DistributionClient(IAuthHandler tokenSource, ICacheFactory cacheFactory)
        {
            _TokenSource = tokenSource;
            _CacheFactory = cacheFactory;
        }

        public Task<T> Get<T>(Uri uri, string accept = null)
        {
            var result = Get(uri, accept);
            var json = result.Content.ReadAsStringAsync().Result;
            return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
        }

        public bool IsDockerHub => Registry.DockerHubAliases.Contains(Host);

        public HttpResponseMessage Get(Uri uri, string accept = null) => Get(uri, accept, retries: 3);

        private HttpResponseMessage Get(Uri uri, string accept, int retries)
        {
            //work out the basic scope + action we'd need to perform this GET
            string scope = null;
            if (_TokenSource.TryParseScope(uri, out var scopePath))
            {
                var action = scopePath == "registry:catalog" ? "*" : "pull";
                scope = $"{scopePath}:{action}";
            }

            using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                client.Timeout = Timeout;
                HttpResponseMessage result;
                var message = new HttpRequestMessage { RequestUri = uri };
                message.Headers.Authorization = _TokenSource.GetAuthorization(scope);

                if (!string.IsNullOrEmpty(accept)) { message.Headers.Add("Accept", accept); }

                result = client.SendAsync(message).Result;

                if (result.IsSuccessStatusCode)
                {
                    return result;
                }
                else if (retries > 0)
                {
                    if (result.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(scope))
                    {
                        var authRequest = _TokenSource.ParseWwwAuthenticate(result.Headers.WwwAuthenticate.First());
                        if (authRequest.scope != scope) { throw new ArgumentException($"The scope requested by the server ({authRequest.scope}) does not match that expected by the auth engine ({scope})"); }
                        // skip service check for dockerhub, since it returns inconsistent values
                        if (!IsDockerHub && authRequest.service != Host) { throw new ArgumentException($"The service indicated by the server ({authRequest.service}), does not match that expected by the auth engine ({Host})."); }

                        if (_TokenSource.UpdateAuthorization(authRequest.scope))
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
                    throw new Exception($"The remote request failed with status {result.StatusCode}");
                }
            }
        }

        public Task<HttpResponseMessage> GetBlobAsync(string repository, string digest)
        {
            return Task.FromResult(Get(new Uri($"https://{Host}/v2/{repository}/blobs/{digest}")));
        }

        public Task<RepositoryList> GetRepositoriesAsync()
        {
            var cache = _CacheFactory?.Get<RepositoryList>();
            RepositoryList list;
            if (_TokenSource.Authorize("registry:catalog:*"))
            {
                if (cache != null && cache.TryGet(Host + ":repos", out list))
                {
                    return Task.FromResult(list);
                }
                else
                {
                    list = Get<RepositoryList>(new Uri(Registry.HostToEndpoint(Host, "_catalog"))).Result;
                    cache?.Set(Host + ":repos", list);
                    return Task.FromResult(list);
                }
            }
            else
            {
                throw new AuthenticationException("The request could not be authorized.");
            }
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
