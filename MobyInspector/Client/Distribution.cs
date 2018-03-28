using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MobyInspector.Data;
using Newtonsoft.Json;

namespace MobyInspector.Client
{
    public class Distribution : IDistribution
    {
        private IMiniClient _Client;
        private string _Host;

        public Distribution(IMiniClient client, string host)
        {
            _Client = client;
            _Host = host;
        }

        public Task<HttpResponseMessage> GetBlobAsync(string repository, string digest)
        {
            var uri = new Uri($"https://{_Host}/v2/{repository}/blobs/{digest}");
            return Task.FromResult(_Client.Get(uri));
        }

        public Task<RepositoryList> GetRepositoriesAsync()
        {
            var uri = new Uri($"https://{_Host}/v2/_catalog");
            var result = _Client.Get(uri);
            var json = result.Content.ReadAsStringAsync().Result;
            return Task.FromResult(JsonConvert.DeserializeObject<RepositoryList>(json));
        }

        public Task<TagList> GetTagsAsync(string repository)
        {
            var uri = new Uri($"https://{_Host}/v2/{repository}/tags/list");
            var result = _Client.Get(uri);
            var json = result.Content.ReadAsStringAsync().Result;
            return Task.FromResult(JsonConvert.DeserializeObject<TagList>(json));
        }

        public Task<HttpResponseMessage> GetV2Manifest(string repository, string tag)
        {
            var uri = new Uri($"https://{_Host}/v2/{repository}/manifests/{tag}");            
            return Task.FromResult(_Client.Get(uri, "application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json"));
        }
    }
}
