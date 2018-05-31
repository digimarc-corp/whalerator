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
using Whalerator.Model;

namespace Whalerator.Client
{
    public class DistributionClient : IDistributionClient
    {
        private IAuthHandler _TokenSource;

        public TimeSpan Timeout { get; set; } = new TimeSpan(0, 3, 0);
        public string Host { get; set; } = Registry.DockerHub;

        public DistributionClient(IAuthHandler tokenSource)
        {
            _TokenSource = tokenSource;
        }

        #region public methods

        public Task<HttpResponseMessage> GetBlobAsync(string repository, string digest)
        {
            return Task.FromResult(Get(new Uri($"https://{Host}/v2/{repository}/blobs/{digest}")));
        }

        public Task<RepositoryList> GetRepositoriesAsync()
        {
            var list = Get<RepositoryList>(new Uri(Registry.HostToEndpoint(Host, "_catalog"))).Result;

            // workaround for https://github.com/docker/distribution/issues/2434
            //list.Repositories = list.Repositories.AsParallel().Where(r => Try(() => GetTagsAsync(r).Wait()));

            return Task.FromResult(list);
        }

        public Task<TagList> GetTagsAsync(string repository)
        {
            try
            {
                return Get<TagList>(new Uri(Registry.HostToEndpoint(Host, $"{repository}/tags/list")));
            }
            catch (NotFoundException)
            {
                return Task.FromResult(new TagList());
            }
        }

        public Task<IEnumerable<Image>> GetImages(string repository, string tag)
        {
            var images = new List<Image>();
            var uri = new Uri($"https://{Host}/v2/{repository}/manifests/{tag}");
            var response = Get(uri, "application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json");

            if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.list.v2+json")
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var fatManifest = JsonConvert.DeserializeObject<FatManifest>(json);
                foreach (var subManifest in fatManifest.Manifests)
                {
                    images.AddRange(GetImages(repository, subManifest.Digest).Result);
                }
            }
            else if (response.Content.Headers.ContentType.MediaType == "application/vnd.docker.distribution.manifest.v2+json")
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var manifest = JsonConvert.DeserializeObject<ManifestV2>(json);
                var config = GetImageConfig(repository, manifest.Config.Digest);
                if (config == null) { throw new NotFoundException("The requested manifest does not exist in the registry."); }
                var image = new Image
                {
                    History = config.History.Select(h => new Model.History { Command = new[] { h.Created_By }, Created = h.Created }),
                    Layers = manifest.Layers.Select(l => l.ToLayer()),
                    Digest = response.Headers.First(h => h.Key.ToLowerInvariant() == "docker-content-digest").Value.First(),
                    Platform = new Platform
                    {
                        Architecture = config.Architecture,
                        OS = config.OS,
                        OSVerion = config.OSVersion
                    }
                };
                image.Layers = manifest.Layers.Select(l => l.ToLayer());

                images.Add(image);
            }
            else
            {
                throw new Exception($"Cannot build image set from mediatype '{response.Content.Headers.ContentType.MediaType}'");
            }

            return Task.FromResult((IEnumerable<Image>)images);
        }

        #endregion

        private Task<T> Get<T>(Uri uri, string accept = null)
        {
            var result = Get(uri, accept);
            var json = result.Content.ReadAsStringAsync().Result;
            return Task.FromResult(JsonConvert.DeserializeObject<T>(json));
        }

        private HttpResponseMessage Get(Uri uri, string accept = null) => Get(uri, accept, retries: 3);

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

                try
                {
                    result = client.SendAsync(message).Result;
                }
                catch
                {
                    if (retries > 0) { result = Get(uri, accept, retries - 1); }
                    else { throw; }
                }

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
                        //if (!IsDockerHub && authRequest.service != Host) { throw new ArgumentException($"The service indicated by the server ({authRequest.service}), does not match that expected by the auth engine ({Host})."); }

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
                    else if (result.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException();
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

        /// <summary>
        /// Runs a throwaway action and returns true if it succeeded, false, if it threw an exception
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        bool Try(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private ImageConfig GetImageConfig(string repository, string digest)
        {
            var result = GetBlobAsync(repository, digest).Result;
            var json = result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<ImageConfig>(json);
        }
    }
}
