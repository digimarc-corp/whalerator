using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Model
{
    /// <summary>
    /// Minimal information necessary to actually retrieve a path from an image
    /// </summary>
    public class LayerPath
    {
        public Layer Layer { get; set; }
        public bool IsDirectory { get; set; }
    }
}
