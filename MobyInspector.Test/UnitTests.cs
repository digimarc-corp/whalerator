using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobyInspector.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void CanEquatePlatforms()
        {
            var p1 = new Platform { Architecture = "x86", OS = "msdos" };
            var p2 = new Platform { Architecture = "x86", OS = "msdos" };
            var p3 = new Platform { Architecture = "ppc", OS = "minix" };

            Assert.IsTrue(p1 == p2);
            Assert.IsTrue(p1 != p3);
        }

        [TestMethod]
        public void PatheratorCanHandleUnixPath()
        {
            var pather = new Patherator();
            var search = pather.Parse("/var/data/myfile");

            Assert.AreEqual("var/data/myfile", search.searchPath);
            Assert.AreEqual("var/data/.wh.myfile", search.fileWhiteout);
            Assert.AreEqual("var/data/.wh..wh.opq", search.pathWhiteout);
        }

        [TestMethod]
        public void PatheratorCanHandleWindowsPath()
        {
            var pather = new Patherator();
            var search = pather.Parse(@"Program Files\CoolApp\license.txt");

            Assert.AreEqual("Program Files/CoolApp/license.txt", search.searchPath);
            Assert.AreEqual("Program Files/CoolApp/.wh.license.txt", search.fileWhiteout);
            Assert.AreEqual("Program Files/CoolApp/.wh..wh.opq", search.pathWhiteout);
        }

        [TestMethod]
        public void PatheratorCanHandleRootedFile()
        {
            var pather = new Patherator();
            var search = pather.Parse(@"readme.md");

            Assert.AreEqual("readme.md", search.searchPath);
            Assert.AreEqual(".wh.readme.md", search.fileWhiteout);
            Assert.AreEqual(".wh..wh.opq", search.pathWhiteout);
        }
    }
}
