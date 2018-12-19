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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Data
{
    public class ImageConfig
    {
        public string Architecture { get; set; }
        public ContainerConfig Config { get; set; }
        public string Container { get; set; }
        public ContainerConfig Container_config { get; set; }
        public DateTime Created { get; set; }
        public string Docker_Version { get; set; }
        public IEnumerable<History> History { get; set; }
        public string OS { get; set; }
        [JsonProperty("os.version")]
        public string OSVersion { get; set; }
        /*"rootfs": {
            "type": "layers",
            "diff_ids": [
                "sha256:e1df5dc88d2cc2cd9a1b1680ec3cb92a2dc924a0205125d85da0c61083b4e87d",
                "sha256:9c2a08e1e47a6c26845a5d16322cee940e52b47e35b0624098212c7aced10869",
                "sha256:f9d40f369825282cf6a599c5f348a0e3784008b401f307cff35a7db48c4c0e52",
                "sha256:b196c7c99e82f2eb35e69cfa5cd1effdee931c59f2966a1d7cbfeab7a1abe1b4"
            ]
        }*/

    }
}
