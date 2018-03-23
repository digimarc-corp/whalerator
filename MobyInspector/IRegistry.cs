using System.Collections.Generic;
using System.IO;
using MobyInspector.Data;

namespace MobyInspector
{
    public interface IRegistry
    {
        Stream GetLayer(string repository, string digest);
        byte[] GetLayerFile(string repository, string digest, string path, bool ignoreCase = true);
        IEnumerable<string> GetLayerFiles(string repository, string digest);
        Manifest GetManifest(string repository, string tag);
        IEnumerable<string> GetRepositories();
        IEnumerable<string> GetTags(string repository);
    }
}