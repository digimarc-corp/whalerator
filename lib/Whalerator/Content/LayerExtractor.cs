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

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Whalerator.Content
{
    public class LayerExtractor : ILayerExtractor
    {
        public Stream ExtractFile(string layerFile, string path)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Extracts a raw list of file entries from a tar'd/gzipped layer
        /// </summary>
        /// <param name="layerArchive"></param>
        /// <returns></returns>
        public IEnumerable<string> ExtractFiles(Stream stream)
        {
            using (var gzipStream = new GZipInputStream(stream))
            using (var tarStream = new TarInputStream(gzipStream))
            {
                var entry = tarStream.GetNextEntry();
                while (entry != null)
                {
                    if (!entry.IsDirectory)
                    {
                        yield return entry.Name;
                    }
                    entry = tarStream.GetNextEntry();
                }
            }
        }
    }
}
