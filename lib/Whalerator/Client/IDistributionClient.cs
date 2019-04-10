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

using Whalerator.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Whalerator.Model;
using System.Net.Http.Headers;

namespace Whalerator.Client
{
    public interface IDistributionClient
    {
        string Host { get; }
        Task<RepositoryList> GetRepositoriesAsync();
        Task<TagList> GetTagsAsync(string repository);
        Task<HttpResponseMessage> GetBlobAsync(string repository, string digest);
        Task<(Uri path, AuthenticationHeaderValue auth)> GetBlobPathAndAuthorizationAsync(string repository, string digest);
        Task<ImageSet> GetImageSet(string repository, string tag);
        Task DeleteImage(string repository, string digest);
    }
}