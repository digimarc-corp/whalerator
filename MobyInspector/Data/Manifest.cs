using System.Collections.Generic;

namespace MobyInspector.Data
{
    public class Manifest
    {
        public int SchemaVersion { get; set; }
        public string MediaType { get; set; }
        public MediaDigest Config { get; set; }
        public IEnumerable<MediaDigest> Layers { get; set; }
    }
}