using System.Collections.Generic;

namespace MobyInspector.Model
{
    public class Image
    {
        public Platform Platform { get; set; }
        public string ManifestDigest { get; set; }
        public IEnumerable<Layer> Layers { get; set; }
        public IEnumerable<History> History { get; set; }
    }
}