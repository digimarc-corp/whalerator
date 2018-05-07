using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Model
{
    public class Repository
    {
        public string Name { get; set; }
        public bool Push { get; set; }
        public bool Pull { get; set; }
        public bool Delete { get; set; }
    }
}
