using MobyInspector.Data;
using Refit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MobyInspector
{
    /// <summary>
    /// Refit wrapper definition for Docker Distribution/Registry API V2
    /// </summary>
    internal interface IDistribution
    {
        [Get("/v2/_catalog")]
        Task<RepositoryList> GetRepositoriesAsync();
        [Get("/v2/_catalog")]
        Task<RepositoryList> GetRepositoriesAsync([Header("Authorization")] string authorization);

        [Get("/v2/{repository}/tags/list")]
        Task<TagList> GetTagsAsync(string repository);
        [Get("/v2/{repository}/tags/list")]
        Task<TagList> GetTagsAsync(string repository, [Header("Authorization")] string authorization);

        [Headers("Accept: application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json")]
        [Get("/v2/{repository}/manifests/{tag}")]
        Task<HttpResponseMessage> GetManifest(string repository, string tag);
        [Headers("Accept: application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json")]
        [Get("/v2/{repository}/manifests/{tag}")]
        Task<HttpResponseMessage> GetManifest(string repository, string tag, [Header("Authorization")] string authorization);

        [Get("/v2/{repository}/blobs/{digest}")]
        Task<HttpResponseMessage> GetLayerAsync(string repository, string digest);
        [Get("/v2/{repository}/blobs/{digest}")]
        Task<HttpResponseMessage> GetLayerAsync(string repository, string digest, [Header("Authorization")] string authorization);
    }
}
