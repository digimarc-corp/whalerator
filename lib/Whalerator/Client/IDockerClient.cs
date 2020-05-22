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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Whalerator.Content;
using Whalerator.Model;

namespace Whalerator.Client
{
    public interface IDockerClient
    {
        IEnumerable<Repository> GetRepositories();
        IEnumerable<string> GetTags(string repository);
        string GetTagDigest(string repository, string tag);
        ImageSet GetImageSet(string repository, string tag);
        Data.ImageConfig GetImageConfig(string repository, string digest);
        LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer, IEnumerable<(string External, string Internal)> aliases);
        Stream GetFile(string repository, Layer layer, string path);
        Layer GetLayer(string repository, string layerDigest);
        IEnumerable<LayerIndex> GetIndexes(string repository, Image image, string target = null);
        Permissions GetPermissions(string repository);
        void DeleteImage(string repository, string imageDigest);
        void DeleteRepository(string repository);
        Stream GetLayerArchive(string repository, string layerDigest);
    }
}
