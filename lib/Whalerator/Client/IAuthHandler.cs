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
using System.Net.Http.Headers;
using Whalerator.Model;

namespace Whalerator.Client
{
    public interface IAuthHandler
    {
        bool AnonymousMode { get; }
        AuthenticationHeaderValue GetAuthorization(string scope);
        bool Authorize(string scope);
        bool HasAuthorization(string scope);
        void Login(string registryHost, string username = null, string password = null);
        string ParseScope(Uri uri);
        (string realm, string service, string scope) ParseWwwAuthenticate(AuthenticationHeaderValue header);
        bool TryParseScope(Uri uri, out string scope);
        bool UpdateAuthorization(string scope, bool force = false);
        string Username { get; }
    }
}