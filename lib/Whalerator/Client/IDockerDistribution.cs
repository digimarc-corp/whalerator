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

using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Data;

namespace Whalerator.Client
{
    public interface IDockerDistribution
    {
        [Get("/_catalog")]
        Task<RepositoryList> GetRepositoryListAsync();

        [Get("/{repository}/tags/list")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<TagList> GetTagListAsync(string repository);

        [Head("/{repository}/manifests/{tag}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        [Headers("Accept: application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json")]
        Task<ApiResponse<object>> GetTagDigest(string repository, string tag);

        // manifest format is unpredictable, so we get it as a plain string here and parse it out later
        [Get("/{repository}/manifests/{tag}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        [Headers("Accept: application/vnd.docker.distribution.manifest.list.v2+json, application/vnd.docker.distribution.manifest.v2+json")]
        Task<string> GetManifest(string repository, string tag);

        [Get("/{repository}/blobs/{digest}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<string> GetStringBlob(string repository, string digest);
        
        [Get("/{repository}/blobs/{digest}")]
        [QueryUriFormat(UriFormat.Unescaped)]
        Task<Stream> GetStreamBlob(string repository, string digest);
    }
}
