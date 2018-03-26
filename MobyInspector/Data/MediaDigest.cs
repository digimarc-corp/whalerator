using System.Collections.Generic;

namespace MobyInspector.Data
{
    public class MediaDigest
    {
        public string MediaType { get; set; }
        public int Size { get; set; }
        public string Digest { get; set; }
        public IEnumerable<string> Urls { get; set; }
    }
}