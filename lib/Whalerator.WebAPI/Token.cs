using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class Token
    {
        public string Crd { get; set; }
        public string Usr { get; set; }
        public string Reg { get; set; }
        public long Iat { get; set; }
        public long Exp { get; set; }
    }
}
