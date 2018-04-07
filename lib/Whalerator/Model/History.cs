using System;
using System.Collections.Generic;

namespace Whalerator.Model
{
    public class History
    {
        public IEnumerable<string> Command { get; set; }
        public DateTime Created { get; set; }
    }
}