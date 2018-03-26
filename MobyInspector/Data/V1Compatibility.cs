using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Data
{
    public class V1Compatibility
    {

        public string architecture { get; set; }
        public ContainerConfig Container_config    {get;set;}
        public string Domainname { get; set; }
        public string User { get; set; }
        public bool AttachStdin { get; set; }
        public bool AttachStdout { get; set; }
        public bool AttachStderr { get; set; }
        public bool Tty { get; set; }
        public bool OpenStdin { get; set; }
        public bool StdinOnce { get; set; }
        public IEnumerable<string> Env { get; set; }
        public IEnumerable<string> Cmd { get; set; }
        public string Image { get; set; }
        //public string Volumes\":null,                 {get;set;}
        public string WorkingDir { get; set; }
        public string Entrypoint { get; set; }
        //public string OnBuild\":null,                 {get;set;}
        //public string Labels\":null},                 {get;set;}
        public DateTime created { get; set; }
        public string id { get; set; }
        public string os { get; set; }
        [JsonProperty("os.version")]
        public string osversion { get; set; }
        public string parent { get; set; }

    }
}
