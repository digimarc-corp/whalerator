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
using System.Text;
using Whalerator.Client;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public abstract class DockerClientBase
    {
        public DockerClientBase(IAuthHandler auth)
        {
            this.AuthHandler = auth;
        }

        protected readonly IAuthHandler AuthHandler;
        public string Host { get; set; }

        protected string CatalogKey(string user) => $"{(string.IsNullOrEmpty(AuthHandler.Username) ? "anon" : AuthHandler.Username)}:{Host}:catalog";

        protected string RepoTagsKey(string repository) => $"{Host}:repository:{repository}:tags";
        protected string RepoTagDigestKey(string repository, string tag) => $"{Host}:repository:{repository}:{tag}";

        public Permissions GetPermissions(string repository)
        {
            if (AuthHandler.Authorize(AuthHandler.RepoAdminScope(repository))) { return Permissions.Admin; }
            else if (AuthHandler.Authorize(AuthHandler.RepoPushScope(repository))) { return Permissions.Push; }
            else if (AuthHandler.Authorize(AuthHandler.RepoPullScope(repository))) { return Permissions.Pull; }
            else { return Permissions.None; }
        }
    }
}
