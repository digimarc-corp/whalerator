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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;

namespace Whalerator.DockerClient
{
    public abstract class DockerClientBase
    {
        public DockerClientBase(ServiceConfig config, IAuthHandler auth)
        {
            this.Config = config;
            this.AuthHandler = auth;
        }

        protected readonly ServiceConfig Config;
        protected readonly IAuthHandler AuthHandler;
        public string Host { get; set; }

        protected string CatalogKey() => $"{(string.IsNullOrEmpty(AuthHandler.Username) ? "anon" : AuthHandler.Username)}:{Host}:catalog";

        protected string RepoTagsKey(string repository) => $"{Host}:repository:{repository}:tags";
        protected string RepoTagDigestKey(string repository, string tag) => $"{Host}:repository:{repository}:{tag}";

        protected async Task<string> ReadFileAsync(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sr = new StreamReader(fs))
            {
                return await sr.ReadToEndAsync();
            }
        }

        protected async Task WriteFileAsync(string path, string content)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8, 4096, true))
                {
                    await sw.WriteAsync(content);
                }
                ms.Seek(0, SeekOrigin.Begin);
                await WriteFileAsync(path, ms);
            }
        }

        protected async Task WriteFileAsync(string path, Stream stream)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs);
            }
        }

        public async Task<Permissions> GetPermissionsAsync(string repository)
        {
            if (AuthHandler.Authorize(AuthHandler.RepoAdminScope(repository))) { return Permissions.Admin; }
            else if (AuthHandler.Authorize(AuthHandler.RepoPushScope(repository))) { return Permissions.Push; }
            else if (AuthHandler.Authorize(AuthHandler.RepoPullScope(repository))) { return Permissions.Pull; }
            else { return Permissions.None; }
        }
    }
}
