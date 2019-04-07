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

using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whalerator.Scanner;

namespace Whalerator.Support
{
    public interface IClairAPI
    {
        [Get("/v1/layers/{digest}")]
        Task<ClairLayerResult> GetLayerResult(string digest, [Query]bool vulnerabilities = true);

        [Post("/v1/layers")]
        Task SubmitLayer([Body]ClairLayerRequest request, CancellationToken cancellationToken);
    }

    public class ClairLayerRequest
    {
        public LayerRequest Layer { get; set; }

        public class LayerRequest
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string ParentName { get; set; }
            public string Format { get; set; } = "Docker";
            public LayerRequestHeaders Headers { get; set; }
            public class LayerRequestHeaders
            {
                public string Authorization { get; set; }
            }
        }
    }

    public class ClairLayerResult
    {
        public ClairLayer Layer { get; set; }
        public class ClairLayer
        {
            public string Name { get; set; }
            public string NamespaceName { get; set; }
            public Component[] Features { get; set; }
        }
    }
}
