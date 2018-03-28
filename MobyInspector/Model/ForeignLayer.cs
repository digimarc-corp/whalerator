using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Model
{
    public class ForeignLayer : Layer
    {
        public IEnumerable<string> Urls { get; set; }
    }
}
