using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Model
{
    public class Platform 
    {
        public string Architecture { get; set; }
        public string OS { get; set; }
        [JsonProperty("os.version")]
        public string OSVerion { get; set; }

        public static Platform Linux { get; private set; } = new Platform { Architecture = "amd64", OS = "linux" };
        public static Platform Windows { get; private set; } = new Platform { Architecture = "amd64", OS = "windows" };
       
    }
}
