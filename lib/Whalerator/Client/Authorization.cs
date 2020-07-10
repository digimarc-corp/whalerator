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

namespace Whalerator.Client
{
    public class Authorization
    {
        public string JWT { get; set; }
        public string Realm { get; set; }
        public string Service { get; set; }
        public bool Anonymous { get; set; }
        public bool DockerHub { get; set; }

        public static string CacheKey(string registry, string username, string password, string scope, bool grant = true)
        {
            // including the password in the key hash lets us use cached tokens without needing to actually validate the
            // credentials with the token service every time. 

            var rawKey = $"{registry}:{username}:{password}:{scope}";
            var hash = System.Security.Cryptography.SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            return $"auth:{(grant ? "grant" : "denial")}:{Convert.ToBase64String(hash)}";
        }
    }
}
