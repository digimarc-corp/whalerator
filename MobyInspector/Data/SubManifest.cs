using MobyInspector.Model;

namespace MobyInspector.Data
{
    public class SubManifest
    {
        public string MediaType { get; set; }
        public int Size { get; set; }
        public string Digest { get; set; }
        public Platform Platform { get; set; }
    }
}