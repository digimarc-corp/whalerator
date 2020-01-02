using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Security
{
    public class Result : ResultBase
    {

        public List<Component> VulnerableComponents { get; set; }
        public int TotalComponents { get; set; }
    }
}
