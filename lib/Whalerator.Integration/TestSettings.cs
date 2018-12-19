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

namespace Whalerator.Integration
{
    internal class TestSettings
    {
        public static TestSettings FromFile(string filename) =>
            new YamlDotNet.Serialization.Deserializer().Deserialize<TestSettings>(System.IO.File.ReadAllText(filename));

        [YamlDotNet.Serialization.YamlMember(Alias = "registry")]
        public string Registry { get; set; }
        [YamlDotNet.Serialization.YamlMember(Alias = "user")]
        public string User { get; set; }
        [YamlDotNet.Serialization.YamlMember(Alias = "password")]
        public string Password { get; set; }
        [YamlDotNet.Serialization.YamlMember(Alias = "repository")]
        public string Repository { get; set; }
    }
}
