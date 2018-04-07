using System.Collections.Generic;
using System.IO;
using Whalerator.Model;

namespace Whalerator
{
    public interface IRegistry
    {
        IEnumerable<string> GetRepositories();
        IEnumerable<string> GetTags(string repository);
        IEnumerable<Image> GetImages(string repository, string tag);
        Stream GetLayer(string repository, Layer layer);
        IEnumerable<string> GetFiles(string repository, Layer layer);
        byte[] GetFile(string repository, Layer layer, string path, bool ignoreCase = true);
        (byte[] data, string layerDigest) FindFile(string repository, Image image, string filename, bool ignoreCase = true);
    }
}