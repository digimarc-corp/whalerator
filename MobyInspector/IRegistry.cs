using System.Collections.Generic;
using System.IO;
using MobyInspector.Data;

namespace MobyInspector
{
    public interface IRegistry
    {
        Stream GetLayer(string repository, MediaDigest digest);
        byte[] GetLayerFile(string repository, MediaDigest digest, string path, bool ignoreCase = true);
        IEnumerable<string> GetLayerFiles(string repository, MediaDigest digest);
        ManifestV2 GetManifest(string repository, string tag, Platform platform);
        IEnumerable<string> GetRepositories();
        IEnumerable<string> GetTags(string repository);
        IEnumerable<Platform> GetPlatforms(string repository, string tag);
        (byte[] data, string layerDigest) Find(string filename, string repository, string tag, Platform platform, bool ignoreCase = true);
    }
}