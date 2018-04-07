using System.Collections.Generic;

namespace Whalerator.Data
{
    public class ManifestV2
    {
        public int SchemaVersion { get; set; }
        public string MediaType { get; set; }
        public MediaDigest Config { get; set; }
        public IEnumerable<MediaDigest> Layers { get; set; }
    }
}