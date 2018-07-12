using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Client;
using Whalerator.Support;

namespace Whalerator.Integration
{
    [TestClass]
    public class AuthorizationTests
    {
        TestSettings Settings = TestSettings.FromFile("settings.yaml");

        [TestMethod]
        public void CanLoginAnonymously()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());

            Assert.AreEqual(null, auth.Service);

            auth.Login(Registry.DockerHub);

            Assert.AreEqual("registry.docker.io", auth.Service);
        }

        [TestMethod]
        public void CanHandleFailedAuthorization()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());
            auth.Login(Registry.DockerHub);

            string scope = "registry:catalog:*";
            Assert.IsNull(auth.GetAuthorization(scope));
            Assert.IsFalse(auth.UpdateAuthorization(scope));
            Assert.IsNull(auth.GetAuthorization(scope));
        }

        [TestMethod]
        public void CanHandleOpenRegistry()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());
            auth.Login("http://localhost:5000");

            Assert.IsTrue(auth.AnonymousMode);
        }

        [TestMethod]
        public void CanGetAuthorization()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());
            auth.Login(Registry.DockerHub);

            string scope = "repository:library/ubuntu:pull";
            Assert.IsNull(auth.GetAuthorization(scope));
            Assert.IsTrue(auth.UpdateAuthorization(scope));
            Assert.IsNotNull(auth.GetAuthorization(scope));
        }

        [TestMethod]
        public void CanLoginWithUsername()
        {
            var auth = new AuthHandler(new DictCache<Authorization>());

            Assert.AreEqual(null, auth.Service);

            auth.Login(Settings.Registry, Settings.User, Settings.Password);

            Assert.AreEqual(Settings.Registry, auth.Service);
        }
    }
}
