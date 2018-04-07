using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Data
{
    public class ContainerConfig
    {

        public string Hostname { get; set; }
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
        public bool ArgsEscaped { get; set; }
        public string Image { get; set; }
        //public IEnumerable<string> Volumes { get; set; }
        public string WorkingDir { get; set; }
        //public string Entrypoint": null,
        //public IEnumerable<string> OnBuild { get; set; }
        //public string Labels": {}

    }
}
