using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Model
{
    public class Platform : IEquatable<Platform>
    {
        public string Architecture { get; set; }
        public string OS { get; set; }
        [JsonProperty("os.version")]
        public string OSVerion { get; set; }

        public static Platform Linux { get; private set; } = new Platform { Architecture = "amd64", OS = "linux" };
        public static Platform Windows_10_0_16299 { get; private set; } = new Platform { Architecture = "amd64", OS = "windows", OSVerion = "10.0.16299.309" };

        #region equality crap

        public static bool operator ==(Platform p1, Platform p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Platform p1, Platform p2)
        {
            return !p1.Equals(p2);
        }

        public override string ToString()
        {
            return $"{OS}/{Architecture}:{OSVerion}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Platform);
        }

        public bool Equals(Platform other)
        {
            return ToString() == other?.ToString();
        }

        #endregion
    }
}
