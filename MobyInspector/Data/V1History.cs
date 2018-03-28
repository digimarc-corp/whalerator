using MobyInspector.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class V1History
    {
        public string V1Compatibility { get; set; }

        public History ToHistory()
        {
            var compat = JsonConvert.DeserializeObject<V1Compatibility>(V1Compatibility);
            return new History
            {
                //Command = compat.Cmd,
                Created = compat.created
            };
        }

        public Platform ToPlatform()
        {
            var compat = JsonConvert.DeserializeObject<V1Compatibility>(V1Compatibility);
            if (string.IsNullOrEmpty(compat.os) || string.IsNullOrEmpty(compat.architecture))
            {
                return null;
            }
            else
            {
                return new Platform { Architecture = compat.architecture, OS = compat.os, OSVerion = compat.osversion };
            }
        }
    }
}
