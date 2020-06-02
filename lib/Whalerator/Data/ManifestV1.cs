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
using Whalerator.Model;

namespace Whalerator.Data
{
    public class ManifestV1
    {
        public string Architecture { get; set; }
        public IEnumerable<FsLayerV1> FsLayers { get; set; }
        public IEnumerable<HistoryV1> History { get; set; }

        public class HistoryV1
        {
            public string v1Compatibility { get; set; }

            public HistoryV1Compatibility Parsed
            {
                get
                {
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<HistoryV1Compatibility>(v1Compatibility);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
            }

            public class HistoryV1Compatibility
            {
                public DateTime? Created { get; set; }
                public string Os { get; set; }
                public ContainerConfig Container_Config { get; set; }

                public class ContainerConfig
                {
                    public IEnumerable<string> Cmd { get; set; }
                }

                public Model.History ToHistory() => Model.History.From(this);
            }
        }

        public class FsLayerV1
        {
            public string BlobSum { get; set; }

            public Layer ToLayer() => new Layer
            {
                Digest = BlobSum,
                Size = 0
            };
        }
    }
}
