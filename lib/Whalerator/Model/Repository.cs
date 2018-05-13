using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Model
{
    public class Repository
    {
        public string Name { get; set; }
        public int Tags{get;set;}
        public bool Push{get;set;}
        public bool Delete{get;set;}
    }
}
