using System;
using System.Collections.Generic;

namespace MobyInspector.Model
{
    public class History
    {
        public IEnumerable<string> Command { get; set; }
        public DateTime Created { get; set; }
    }
}