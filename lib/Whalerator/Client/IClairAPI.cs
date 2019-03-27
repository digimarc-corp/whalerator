using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Scanner;

namespace Whalerator.Client
{
    interface IClairAPI
    {
        [Get("/v1/layers/{digest}")]
        Task<ClairLayerResult> GetLayerResult(string digest, [Query]bool vulnerabilities = true);
    }

    public class ClairLayerResult
    {
        public ClairLayer Layer { get; set; }
        public class ClairLayer
        {
            public string Name { get; set; }
            public string NamespaceName { get; set; }
            public Component[] Features { get; set; }
        }
    }
}
