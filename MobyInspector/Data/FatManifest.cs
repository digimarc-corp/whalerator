using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class FatManifest
    {
        public int SchemaVerions { get; set; }
        public string MediaType { get; set; }
        public IEnumerable<SubManifest> Manifests { get; set; }
    }
}
