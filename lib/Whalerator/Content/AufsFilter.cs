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
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;

namespace Whalerator.Content
{
    public class AufsFilter : IAufsFilter
    {
        private Regex whRegex = new Regex(@"^(.*/)\.wh\.([^/]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// If true, targeted searches will terminate on case-insenitive matches
        /// </summary>
        public bool CaseInsensitiveSearch { get; set; } = false;

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
        public async IAsyncEnumerable<LayerIndex> FilterLayers(IAsyncEnumerable<LayerIndex> layers, params string[] targets)
        {
            // master list of all files found, to enable duplicate detection
            var runFiles = new HashSet<string>();

            // prep the list of target paths, if provided
            if (CaseInsensitiveSearch) { targets = targets?.Select(t => t.ToLowerInvariant())?.ToArray(); }
            var targetsHash = targets.Length == 0 ? null : new HashSet<string>(targets.Select(t => t.TrimStart('/')));

            int currentDepth = 0;

            var wh = new List<string>();

            LayerIndex newLayer = null;

            await foreach (var layer in layers)
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
                var newFiles = layerFiles.Where(lf => !(lf.isWh || MatchesAnyWh(lf.path, wh) || runFiles.Any(xf => xf.Equals(lf.path))))
                    .Select(f => f.path).ToList();
                runFiles.UnionWith(newFiles);

                // check for target hits
                if (targetsHash != null)
                {
                    var hits = newFiles.Select(f => CaseInsensitiveSearch ? f.ToLowerInvariant() : f).Where(f => targetsHash.Contains(f)).ToHashSet();
                    targetsHash.RemoveWhere(t => hits.Contains(t));
                }

                // add any new whiteouts
                wh.AddRange(layerFiles.Where(f => f.isWh).Select(f => f.path));

                // if we found something, create a new layer index
                newLayer = newFiles.Count > 0 ? new LayerIndex
                {
                    Digest = layer.Digest,
                    Depth = currentDepth,
                    Files = newFiles
                } : null;

                // if we still have work to do, return the layer and continue iterating
                if (targetsHash == null || targetsHash.Count > 0)
                {
                    if (newLayer != null)
                    {
                        yield return newLayer;
                    }
                }
                else
                {
                    // we're done searching, break the loop before yielding the final result
                    break;
                }
            }

            if (newLayer != null)
            {
                yield return newLayer;
            }
        }
    }
}
