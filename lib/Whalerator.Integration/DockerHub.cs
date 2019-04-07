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
using System.IO;
using System.Linq;
using System.Text;
using Whalerator.Client;

namespace Whalerator.Integration
{
    [TestClass]
    public class DockerHub
    {
        IRegistry dockerHub = new Registry(new RegistryCredentials { Registry = "registry-1.docker.io" },
            new RegistrySettings { LayerCache = @"c:\layercache", AuthHandler = new AuthHandler(new DictCache<Authorization>()) });

        TestSettings Settings = TestSettings.FromFile("settings.yaml");

        [TestMethod]
        public void CanGetRepoList()
        {
            var dmrcIo = new Registry(
                new RegistryCredentials { Registry = Settings.Registry, Username = Settings.User, Password = Settings.Password },
                new RegistrySettings { LayerCache = @"c:\layercache", AuthHandler = new AuthHandler(new DictCache<Authorization>()) });

            var x = dmrcIo.GetRepositories();
            Assert.IsTrue(x.Count() > 1);

            var y = dmrcIo.GetImages("bdi", "latest", false);
            var z = dmrcIo.GetFiles("bdi", y.First().Layers.Last());
        }

        [TestMethod]
        public void CanGetWindowsImageSet()
        {
            var images = dockerHub.GetImages("microsoft/nanoserver", "1709", false);
            Assert.AreEqual(1, images.Count());
            Assert.AreEqual(Platform.Windows.OS, images.First().Platform.OS);
        }

        [TestMethod]
        public void CanGetMixedImageSet()
        {
            var images = dockerHub.GetImages("microsoft/dotnet", "2.0-runtime", false);
            Assert.IsTrue(images.Count() >= 4);
            Assert.IsTrue(images.Any(i => i.Platform.OS == Platform.Windows.OS));
            Assert.IsTrue(images.Any(i => i.Platform.OS == Platform.Linux.OS));
        }

        [TestMethod]
        public void CanGetTagList()
        {
            var x = dockerHub.GetTags("library/alpine");
            Assert.IsTrue(x.Count() > 1);
        }

        [TestMethod]
        public void CanGetForeignLayerFiles()
        {
            var image = dockerHub.GetImages("microsoft/nanoserver", "1709", false).Where(i => i.Platform.OS == Platform.Windows.OS).First();
            var layer = image.Layers.First();
            var files = dockerHub.GetFiles("microsoft/nanoserver", layer);
            Assert.IsTrue(files.Count() > 1000);

            var license = dockerHub.GetFile("microsoft/nanoserver", layer, "Files/License.txt");
            var licenseText = Encoding.UTF8.GetString(license);
            Assert.IsTrue(licenseText.StartsWith("MICROSOFT"));
        }

        [TestMethod]
        public void CanGetLayerData()
        {
            var image = dockerHub.GetImages("library/ubuntu", "16.04", false).Where(i => i.Platform.OS == Platform.Linux.OS).First();
            var stream = dockerHub.GetLayer("library/ubuntu", image.Layers.Last());
            Assert.IsTrue(stream.Length > 1);
        }

        [TestMethod]
        public void CanGetLayerFiles()
        {
            var image = dockerHub.GetImages("library/alpine", "3.4", false).Where(i => i.Platform.OS == Platform.Linux.OS).First();
            var x = dockerHub.GetFiles("library/alpine", image.Layers.Last());
            Assert.IsTrue(x.Count() > 1);
        }

        [TestMethod]
        public void CanGetLayerFile()
        {
            var manifest = dockerHub.GetImages("library/redis", "4.0-alpine", false).First();
            var file = dockerHub.GetFile("library/redis", manifest.Layers.ToList()[0], "etc/motd");
            var copyright = Encoding.UTF8.GetString(file);
            Assert.IsTrue(copyright.Contains(@"Welcome to Alpine!"));
        }

        [TestMethod]
        public void CanFindFile()
        {
            var image = dockerHub.GetImages("library/ubuntu", "16.04", false).First();
            var layer = dockerHub.FindFile("library/ubuntu", image, "/bin/bash");
            var data = dockerHub.GetFile("library/ubuntu", layer, "bin/bash");
            var hash = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(data)).ToLower().Replace("-", string.Empty);

            Assert.AreEqual("3d74ea46393deee0ff74bfbfd9479242b090f20ebffe9a27f572679f277153ed", hash);
        }
    }
}
