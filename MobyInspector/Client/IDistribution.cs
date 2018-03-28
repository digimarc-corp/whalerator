using MobyInspector.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MobyInspector.Client
{
    /// <summary>
    /// Refit wrapper definition for Docker Distribution/Registry API V2
    /// </summary>
    public interface IDistribution
    {
        Task<RepositoryList> GetRepositoriesAsync();

        Task<TagList> GetTagsAsync(string repository);

        Task<HttpResponseMessage> GetV2Manifest(string repository, string tag);

        Task<HttpResponseMessage> GetBlobAsync(string repository, string digest);
    }
}
