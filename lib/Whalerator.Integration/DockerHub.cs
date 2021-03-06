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
        IRegistry dockerHub = new Registry(
            credentials: new RegistryCredentials { Registry = "registry-1.docker.io" },
            config: new RegistrySettings { LayerCache = @"c:\layercache", AuthHandler = new AuthHandler(new DictCache<Authorization>()) },
            logger: null);

        //TestSettings Settings = TestSettings.FromFile("settings.yaml");

        [TestMethod]
        public void CanGetWindowsImageSet()
        {
            var imageSet = dockerHub.GetImageSet("microsoft/nanoserver", "1709", false);
            Assert.AreEqual(1, imageSet.Images.Count());
            Assert.AreEqual(Platform.Windows.OS, imageSet.Images.First().Platform.OS);
        }

        [TestMethod]
        public void CanGetMixedImageSet()
        {
            var imageSet = dockerHub.GetImageSet("microsoft/dotnet", "2.0-runtime", false);
            Assert.IsTrue(imageSet.Images.Count() >= 4);
            Assert.IsTrue(imageSet.Images.Any(i => i.Platform.OS == Platform.Windows.OS));
            Assert.IsTrue(imageSet.Images.Any(i => i.Platform.OS == Platform.Linux.OS));
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
            var image = dockerHub.GetImageSet("microsoft/nanoserver", "1709", false).Images.Where(i => i.Platform.OS == Platform.Windows.OS).First();
            var layer = image.Layers.First();
            var files = dockerHub.GetPaths("microsoft/nanoserver", layer);
            Assert.IsTrue(files.Count() > 1000);

            var license = dockerHub.GetFile("microsoft/nanoserver", layer, "Files/License.txt");
            var licenseText = Encoding.UTF8.GetString(license);
            Assert.IsTrue(licenseText.StartsWith("MICROSOFT"));
        }

        [TestMethod]
        public void CanGetLayerData()
        {
            var image = dockerHub.GetImageSet("library/ubuntu", "16.04", false).Images.Where(i => i.Platform.OS == Platform.Linux.OS).First();
            var stream = dockerHub.GetLayer("library/ubuntu", image.Layers.Last());
            Assert.IsTrue(stream.Length > 1);
        }

        [TestMethod]
        public void CanGetLayerFiles()
        {
            var image = dockerHub.GetImageSet("library/alpine", "3.4", false).Images.Where(i => i.Platform.OS == Platform.Linux.OS).First();
            var x = dockerHub.GetPaths("library/alpine", image.Layers.Last());
            Assert.IsTrue(x.Count() > 1);
        }

        [TestMethod]
        public void CanGetLayerFile()
        {
            var manifest = dockerHub.GetImageSet("library/redis", "4.0-alpine", false).Images.First();
            var file = dockerHub.GetFile("library/redis", manifest.Layers.ToList()[0], "etc/motd");
            var copyright = Encoding.UTF8.GetString(file);
            Assert.IsTrue(copyright.Contains(@"Welcome to Alpine!"));
        }

        [TestMethod]
        public void CanFindFile()
        {
            var image = dockerHub.GetImageSet("library/ubuntu", "16.04", false).Images.First();
            var layerPath = dockerHub.FindPath("library/ubuntu", image, "/bin/bash");
            var data = dockerHub.GetFile("library/ubuntu", layerPath.Layer, "bin/bash");
            var hash = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(data)).ToLower().Replace("-", string.Empty);

            Assert.AreEqual("3d74ea46393deee0ff74bfbfd9479242b090f20ebffe9a27f572679f277153ed", hash);
        }
    }
}
