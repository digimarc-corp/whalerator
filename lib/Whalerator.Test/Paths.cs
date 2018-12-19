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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Whalerator.Data;
using Whalerator.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Client;
using Whalerator.Support;

namespace Whalerator.Test
{
    [TestClass]
    public class Paths
    {
        [TestMethod]
        public void CanParseUriPathToScope()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());
            Assert.AreEqual("registry:catalog", auth.ParseScope(new Uri("https://myregistry.io/v2/_catalog")));
            Assert.AreEqual("repository:coolimage", auth.ParseScope(new Uri("https://myregistry.io/v2/coolimage/tags/list")));
            Assert.AreEqual("repository:cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/cool/image/manifests/latest")));
            Assert.AreEqual("repository:super/cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/super/cool/image/blobs/sha256:digest")));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CanThrowOnBadUriPath()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());
            auth.ParseScope(new Uri("https://myregistry.io/something/else"));
        }

        [TestMethod]
        public void PatheratorCanHandleUnixPath()
        {
            var search = "/var/data/myfile".Patherate();

            Assert.AreEqual("var/data/myfile", search.searchPath);
            Assert.AreEqual("var/data/.wh.myfile", search.fileWhiteout);
            Assert.AreEqual("var/data/.wh..wh.opq", search.pathWhiteout);
        }

        [TestMethod]
        public void PatheratorCanHandleWindowsPath()
        {
            var search = @"Program Files\CoolApp\license.txt".Patherate();

            Assert.AreEqual("Program Files/CoolApp/license.txt", search.searchPath);
            Assert.AreEqual("Program Files/CoolApp/.wh.license.txt", search.fileWhiteout);
            Assert.AreEqual("Program Files/CoolApp/.wh..wh.opq", search.pathWhiteout);
        }

        [TestMethod]
        public void PatheratorCanHandleRootedFile()
        {
            var search = @"readme.md".Patherate();

            Assert.AreEqual("readme.md", search.searchPath);
            Assert.AreEqual(".wh.readme.md", search.fileWhiteout);
            Assert.AreEqual(".wh..wh.opq", search.pathWhiteout);
        }
    }
}
