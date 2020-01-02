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

using System.Collections.Generic;
using System.IO;
using Whalerator.Model;

namespace Whalerator
{
    public interface IRegistry
    {
        IEnumerable<Repository> GetRepositories(bool empties = false);
        IEnumerable<string> GetTags(string repository);
        ImageSet GetImageSet(string repository, string tag, bool isDigest);
        Stream GetLayer(string repository, Layer layer);
        LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer, IEnumerable<(string External, string Internal)> aliases);
        IEnumerable<(string name, bool isDirectory)> GetPaths(string repository, Layer layer);
        byte[] GetFile(string repository, Layer layer, string path, bool ignoreCase = true);
        LayerPath FindPath(string repository, Image image, string filename, int maxDepth = 0, bool ignoreCase = true);
        IEnumerable<string> GetImageFiles(string repository, Image image, int maxDepth);
        Permissions GetPermissions(string repository);
        void DeleteImage(string repository, string digest);
        void DeleteRepository(string repository);
    }
}