using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.Content
{
    public class ContentScanner : IContentScanner
    {
        private ILogger<ContentScanner> _Log;
        private ConfigRoot _Config;
        private ICacheFactory _CacheFactory;
        public IWorkQueue<Request> Queue { get; private set; }

        public ContentScanner(ILogger<ContentScanner> logger, ConfigRoot config, ICacheFactory cacheFactory, IWorkQueue<Request> queue)
        {
            _Log = logger;
            _Config = config;
            _CacheFactory = cacheFactory;
            Queue = queue;
        }

        private string GetKey(Image image, string path) => $"static:content:{image.Digest}:{path}";

        /// <summary>
        /// Returns a cached result from the Index function, or null if the path has not been indexed
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public Result GetPath(Image image, string path)
        {
            var cache = _CacheFactory.Get<Result>();
            var key = GetKey(image, path);

            return cache.TryGet(key, out var cachedResult) ? cachedResult : null;
        }

        /// <summary>
        /// Searches a given image for a specific path, and pushes the result into the cache. If the path is a directory,
        /// the result will be a recursive list of all paths under that directory. If the path is a file, the result will be
        /// the actual file contents.
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="repository"></param>
        /// <param name="image"></param>
        /// <param name="path"></param>
        public void Index(IRegistry registry, string repository, Image image, string path)
        {
            var cache = _CacheFactory.Get<Result>();
            var key = GetKey(image, path);

            // find a pointer to the specific layer that contains the requested path
            var layerPath = registry.FindPath(repository, image, path);

            if (layerPath == null)
            {
                cache.Set(key, new Result { Exists = false });
            }
            else if (layerPath.IsDirectory)
            {
#warning partially implemented
                // currently this ignores the path argument and just returns everything in the image
                var files = registry.GetImageFiles(repository, image, 0).ToList();
                cache.Set(key, new Result { Exists = true, Children = files });
            }
            else
            {
                var data = registry.GetFile(repository, layerPath.Layer, path);
                cache.Set(key, new Result { Exists = true, Content = data });
            }
        }
    }
}
