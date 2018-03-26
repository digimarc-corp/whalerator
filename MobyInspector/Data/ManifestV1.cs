using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class ManifestV1
    {
        public string SchemaVerion { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public string Architecture { get; set; }
        public IEnumerable<V1History> History { get; set; }
    }
}
