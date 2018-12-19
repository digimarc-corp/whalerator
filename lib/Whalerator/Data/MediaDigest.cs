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

using Whalerator.Model;
using System;
using System.Collections.Generic;

namespace Whalerator.Data
{
    public class MediaDigest
    {
        public string MediaType { get; set; }
        public int Size { get; set; }
        public string Digest { get; set; }
        public IEnumerable<string> Urls { get; set; }

        public Layer ToLayer()
        {
            switch (MediaType)
            {
                case "application/vnd.docker.image.rootfs.diff.tar.gzip":
                    return new Layer { Digest = Digest, Size = Size };
                case "application/vnd.docker.image.rootfs.foreign.diff.tar.gzip":
                    return new ForeignLayer { Digest = Digest, Size = Size, Urls = Urls };
                default:
                    throw new Exception($"Can't create a Layer object for mediatype '{MediaType}'");
            }
        }
    }
}