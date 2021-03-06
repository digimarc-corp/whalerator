﻿/*
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
using Whalerator.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Client;
using Whalerator.Support;
using Xunit;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.IO;

namespace Whalerator.Test
{
    public class Paths
    {
        [Theory]
        [InlineData(" Hi! ")]
        [InlineData("Hi! ")]
        [InlineData(" Hi!")]
        [InlineData(" \t\n")]
        [InlineData("# Hi there\n\nHow's it going?")]
        [InlineData("\tGreetings, fellow kids!")]
        [InlineData("")]
        [InlineData(null)]
        public void BannerReaderCanIgnoreNonPaths(string source) =>
            Assert.Equal(source, Banners.ReadBanner(source));

        [Theory]
        [InlineData("/test.md")]
        [InlineData("/test/my.md")]
        [InlineData("/test/a long filename.md")]
        [InlineData("f")]
        public void BannerReaderTriesToReadPaths(string source)
        {
            var ex = Assert.Throws<FileNotFoundException>(() => Banners.ReadBanner(source));
            Assert.Equal(source, ex.FileName);
        }


        [Theory]
        [InlineData("sha256:056e909defe3ff3eb5514843a64521abf0f40b20ff270cc1a394b45bf815698c", "sha256/05/056e909defe3ff3eb5514843a64521abf0f40b20ff270cc1a394b45bf815698c")]
        [InlineData("mydigest:abcd123", "mydigest/ab/abcd123")]
        public void CanParseDigestPath(string digest, string path) =>
            Assert.Equal(path.Replace('/', System.IO.Path.DirectorySeparatorChar), digest.ToDigestPath());

        [Theory]
        [InlineData("notadigest")]
        [InlineData((string)null)]
        [InlineData("")]
        public void CanDetectBadDigest(string badDigest) =>
            Assert.Throws<FormatException>(() => _ = badDigest.ToDigestPath());

        [Fact]
        public void CanParseUriPathToScope()
        {
            var auth = new AuthHandler(new DictCache<Authorization>(), null, null);
            Assert.Equal("registry:catalog", auth.ParseScope(new Uri("https://myregistry.io/v2/_catalog")));
            Assert.Equal("repository:coolimage", auth.ParseScope(new Uri("https://myregistry.io/v2/coolimage/tags/list")));
            Assert.Equal("repository:cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/cool/image/manifests/latest")));
            Assert.Equal("repository:super/cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/super/cool/image/blobs/sha256:digest")));
        }

        [Fact]
        public void CanThrowOnBadUriPath()
        {
            var auth = new AuthHandler(new DictCache<Authorization>(), null, null);
            Assert.Throws<ArgumentException>(() => auth.ParseScope(new Uri("https://myregistry.io/something/else")));
        }

        [Fact]
        public void PatheratorCanHandleUnixPath()
        {
            var search = "/var/data/myfile".Patherate();

            Assert.Equal("var/data/myfile", search.searchPath);
            Assert.Equal("var/data/.wh.myfile", search.fileWhiteout);
            Assert.Equal("var/data/.wh..wh.opq", search.pathWhiteout);
        }

        [Fact]
        public void PatheratorCanHandleWindowsPath()
        {
            var search = @"Program Files\CoolApp\license.txt".Patherate();

            Assert.Equal("Program Files/CoolApp/license.txt", search.searchPath);
            Assert.Equal("Program Files/CoolApp/.wh.license.txt", search.fileWhiteout);
            Assert.Equal("Program Files/CoolApp/.wh..wh.opq", search.pathWhiteout);
        }

        [Fact]
        public void PatheratorCanHandleRootedFile()
        {
            var search = @"readme.md".Patherate();

            Assert.Equal("readme.md", search.searchPath);
            Assert.Equal(".wh.readme.md", search.fileWhiteout);
            Assert.Equal(".wh..wh.opq", search.pathWhiteout);
        }
    }
}
