/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

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
