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
        ImageSet GetImageSet(string repository, string tag);
        LayerProxyInfo GetLayerProxyInfo(string repository, Layer layer, IEnumerable<(string External, string Internal)> aliases);
        Stream GetFile(Layer layer, string path);
        IEnumerable<LayerIndex> GetIndexes(Image image);
        Permissions GetPermissions(string repository);
        void DeleteImage(string repository, string digest);
        void DeleteRepository(string repository);
        Stream GetLayerArchive(string digest);
    }
}
