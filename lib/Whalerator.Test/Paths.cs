using Microsoft.VisualStudio.TestTools.UnitTesting;
using Whalerator.Data;
using Whalerator.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Client;

namespace Whalerator.Test
{
    [TestClass]
    public class Paths
    {
        [TestMethod]
        public void CanParseUriPathToScope()
        {
            var auth = new AuthHandler();
            Assert.AreEqual("registry:catalog", auth.ParseScope(new Uri("https://myregistry.io/v2/_catalog")));
            Assert.AreEqual("repository:coolimage", auth.ParseScope(new Uri("https://myregistry.io/v2/coolimage/tags/list")));
            Assert.AreEqual("repository:cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/cool/image/manifests/latest")));
            Assert.AreEqual("repository:super/cool/image", auth.ParseScope(new Uri("https://myregistry.io/v2/super/cool/image/blobs/sha256:digest")));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CanThrowOnBadUriPath()
        {
            var auth = new AuthHandler();
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
