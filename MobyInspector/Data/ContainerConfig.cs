using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class ContainerConfig
    {
        public IEnumerable<string> Cmd { get; set; }
        public IEnumerable<string> Env { get; set; }
        public bool Throwaway { get; set; }
    }
}
