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
using System.IO;
using System.Text;
using Whalerator.Content;

namespace Whalerator
{
    /* the index store does save some work re-indexing layers, but more importantly
     * it is the relay point for the indexer workers and the api - if there's a record in the
     * store, the api can return it immediately. If not, it needs to queue a request,
     * and a worker will build an index and push in here.
     */
    public class IndexStore : IIndexStore
    {
        public string StoreFolder { get; set; }

        string indexRoot => Path.Combine(StoreFolder, "whalerator/v1/indexes");

        string GetPath(string digest, string target) =>
            Path.Combine(indexRoot, digest.ToDigestPath() + ".json", string.IsNullOrEmpty(target) ? "full" : target.Trim('/'));


        public IEnumerable<LayerIndex> GetIndex(string digest, string target) =>
            JsonConvert.DeserializeObject<List<LayerIndex>>(File.ReadAllText(GetPath(digest, target)));

        public bool IndexExists(string digest, string target) =>
            File.Exists(GetPath(digest, target));

        public void SetIndex(IEnumerable<LayerIndex> index, string digest, string target)
        {
            var path = GetPath(digest, target);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(index));
        }

    }
}
