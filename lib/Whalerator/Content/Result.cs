using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Content
{
    public class Result : ResultBase
    {
        public bool Exists { get; set; }
        public byte[] Content { get; set; }
        public List<string> Children { get; set; }
    }
}
