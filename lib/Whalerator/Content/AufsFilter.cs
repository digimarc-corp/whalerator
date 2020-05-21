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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Whalerator.Client;
using Whalerator.Model;

namespace Whalerator.Content
{
    public class AufsFilter : IAufsFilter
    {
        private Regex whRegex = new Regex(@"^(.*/)\.wh\.([^/]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private bool MatchWh(string path, out Match match)
        {
            match = whRegex.Match(path);
            return match.Success;
        }

        /// <summary>
        /// Decodes a whiteout entry, and returns it's target. Opaque whiteouts are treated as a delete for the parent folder - i.e., empty folders are not preserved.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsWhiteout(string path, out string target)
        {
            if (string.IsNullOrEmpty(path))
            {
                target = path;
                return false;
            }
            else if (path.EndsWith(".wh..wh.opq"))
            {
                target = path[..^12].TrimEnd('/');
                return true;
            }
            else if (MatchWh(path, out var m))
            {
                target = string.Join("", m.Groups[1], m.Groups[2]);
                return true;
            }
            else
            {
                target = path;
                return false;
            }
        }


       
        /// <summary>
        /// Checks if a file path is covered by any known whiteouts
        /// </summary>
        /// <param name="path"></param>
        /// <param name="wh"></param>
        /// <returns></returns>
        public bool MatchesAnyWh(string path, IEnumerable<string> wh) => wh.Any(w => path == w || path.StartsWith(w + "/"));

        /// <summary>
        /// Takes a raw list of LayerIndexes, and processes any duplicated or whited-out entries
        /// </summary>
        /// <param name="fileEntries">Enumerable entries. Must be in top-down order.</param>
        /// <param name="target">If set, processing will stop when the target path is found. Only processed layers will be returned</param>
        /// <returns></returns>
        public IEnumerable<LayerIndex> FilterLayers(IEnumerable<LayerIndex> layers, string target = null)
        {
            var files = new List<(string name, string digest, int depth)>();

            int currentDepth = 0;

            var wh = new List<string>();

            foreach (var layer in layers)
            {
                if (layer.Depth <= currentDepth) { throw new DataMisalignedException("LayerIndexes must enumerate in top-down order."); }
                else { currentDepth = layer.Depth; }

                var layerFiles = layer.Files
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(f => new
                    {
                        isWh = IsWhiteout(f, out var n),
                        path = n,
                    }
                );

                // add any new files not in a whiteout/opq
                var newFiles = layerFiles.Where(lf => !(lf.isWh || MatchesAnyWh(lf.path, wh) || files.Any(xf => xf.name.Equals(lf.path))))
                    .Select(f => (f.path, layer.Digest, layer.Depth)).ToList();
                files.AddRange(newFiles);

                // check for a target hit
                if (newFiles.Any(f => f.path.Equals(target))) { break; }

                // add any new whiteouts
                wh.AddRange(layerFiles.Where(f => f.isWh).Select(f => f.path));
            }

            return files.GroupBy(f => f.digest).Select(g => new LayerIndex
            {
                Digest = g.Key,
                Depth = g.Min(d => d.depth),
                Files = g.Select(f => f.name)
            });
        }
    }
}
