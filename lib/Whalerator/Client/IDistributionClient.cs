using Whalerator.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Whalerator.Client
{
    public interface IDistributionClient
    {
        HttpResponseMessage Get(Uri uri, string accept = null);

        Task<RepositoryList> GetRepositoriesAsync();

        Task<TagList> GetTagsAsync(string repository);

        Task<HttpResponseMessage> GetV2Manifest(string repository, string tag);

        Task<HttpResponseMessage> GetBlobAsync(string repository, string digest);
    }
}