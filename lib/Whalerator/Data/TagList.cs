using System.Collections.Generic;

namespace Whalerator.Data
{
    public class TagList
    {
        public string Name { get; set; }
        public IEnumerable<string> Tags { get; set; } = new string[0];
    }
}