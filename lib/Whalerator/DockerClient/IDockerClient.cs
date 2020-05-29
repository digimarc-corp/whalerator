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
using System.Threading.Tasks;
using Whalerator.Content;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public interface IDockerClient
    {
        IEnumerable<Repository> GetRepositories();
        IEnumerable<string> GetTags(string repository);
        IEnumerable<LayerIndex> GetIndexes(string repository, Image image, params string[] targets);
        Task<string> GetTagDigestAsync(string repository, string tag);
        Task<ImageSet> GetImageSetAsync(string repository, string tag);
        Task<Data.ImageConfig> GetImageConfigAsync(string repository, string digest);
        Task<Stream> GetFileAsync(string repository, Layer layer, string path);
        Task<Layer> GetLayerAsync(string repository, string layerDigest);
        Task<Permissions> GetPermissionsAsync(string repository);
        Task DeleteImageAsync(string repository, string imageDigest);
        Task DeleteRepositoryAsync(string repository);
        Task<Stream> GetLayerArchiveAsync(string repository, string layerDigest);
    }
}
