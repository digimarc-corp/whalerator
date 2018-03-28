using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class History
    {
        public DateTime Created { get; set; }
        public string Created_By { get; set; }
        public bool Empty_Layer { get; set; }
    }
}
