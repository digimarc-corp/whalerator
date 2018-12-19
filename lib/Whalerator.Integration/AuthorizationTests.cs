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
