using System.Collections.Generic;

namespace MobyInspector.Data
{
    public class TagList
    {
        public string Name { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}