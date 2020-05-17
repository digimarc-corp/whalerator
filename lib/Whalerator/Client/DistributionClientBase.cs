using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Data;
using Whalerator.Model;

namespace Whalerator.Client
{
    public abstract class DistributionClientBase : IDistributionClient
    {
        public abstract string Host { get; set; }

        public abstract Task DeleteImage(string repository, string digest);
        public abstract Task<Stream> GetBlobAsync(string repository, string digest);
        public abstract Task<(Uri path, AuthenticationHeaderValue auth)> GetBlobPathAndAuthorizationAsync(string repository, string digest);
        public abstract Task<ImageSet> GetImageSet(string repository, string tag);
        public abstract Task<RepositoryList> GetRepositoriesAsync();
        public abstract Task<TagList> GetTagsAsync(string repository);

        protected ImageConfig GetImageConfig(string repository, string digest)
        {
            var result = GetBlobAsync(repository, digest).Result;
            var json = new StreamReader(result).ReadToEnd();
            return JsonConvert.DeserializeObject<ImageConfig>(json);
        }
    }
}
