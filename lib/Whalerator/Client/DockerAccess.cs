using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Client
{
    public class DockerAccess
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public IEnumerable<string> Actions { get; set; }
    }
}
