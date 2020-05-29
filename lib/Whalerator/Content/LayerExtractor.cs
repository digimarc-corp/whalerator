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
using System.Threading.Tasks;

namespace Whalerator.Content
{
    public class LayerExtractor : ILayerExtractor
    {
        public Task<Stream> ExtractFileAsync(string layerFile, string path)
        {
            path = path.TrimStart('/');

            // can't put in using block as we're (hopefully) returning all these items nested in the final SubStream
            var stream = new FileStream(layerFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var gzipStream = new GZipInputStream(stream);
            var tarStream = new TarInputStream(gzipStream);

            try
            {
                Stream foundStream = null;
                var entry = tarStream.GetNextEntry();
                while (entry != null)
                {
                    if (entry.Name.Equals(path))
                    {
                        foundStream = new SubStream(tarStream, entry.Size) { OwnInnerStream = true };
                        break;
                    }
                    entry = tarStream.GetNextEntry();
                }

                if (foundStream != null)
                {
                    return Task.FromResult(foundStream);
                }
                else
                {
                    // tarStream will dispose the entire chain for us
                    tarStream.Dispose();
                    throw new FileNotFoundException();
                }
            }
            catch (GZipException)
            {
                throw new ArgumentException($"Layer appears invalid");
            }
        }


        /// <summary>
        /// Extracts a raw list of file entries from a tar'd/gzipped layer
        /// </summary>
        /// <param name="layerArchive"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ExtractFilesAsync(Stream stream)
        {
            using (var gzipStream = new GZipInputStream(stream) { IsStreamOwner = false })
            using (var tarStream = new TarInputStream(gzipStream) { IsStreamOwner = false })
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
