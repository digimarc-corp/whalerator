using Whalerator.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Whalerator.Model;

namespace Whalerator.Client
{
    public interface IDistributionClient
    {

        Task<RepositoryList> GetRepositoriesAsync();
        Task<TagList> GetTagsAsync(string repository);
        Task<HttpResponseMessage> GetBlobAsync(string repository, string digest);
        Task<IEnumerable<Image>> GetImages(string repository, string tag);
    }
}