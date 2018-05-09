using System.Collections.Generic;
using System.IO;
using Whalerator.Model;

namespace Whalerator
{
    public interface IRegistry
    {
        IEnumerable<Repository> GetRepositories();
        IEnumerable<string> GetTags(string repository);
        IEnumerable<Image> GetImages(string repository, string tag);
        Stream GetLayer(string repository, Layer layer);
        IEnumerable<string> GetFiles(string repository, Layer layer);
        byte[] GetFile(string repository, Layer layer, string path, bool ignoreCase = true);
        Layer FindFile(string repository, Image image, string filename, bool ignoreCase = true);
        IEnumerable<ImageFile> GetImageFiles(string repository, Image image, int maxDepth);
    }
}