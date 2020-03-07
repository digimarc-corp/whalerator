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

namespace Whalerator.Config
{
    /// <summary>
    /// Unlike the other Config classes, the configuration data here is synthesized from config.yaml to let the UI (or other consumers)
    /// know what features to display and/or enable for the user.
    /// </summary>
    public class PublicConfig
    {
        public string UserTheme { get; set; }
        public string Registry { get; set; }
        public bool AutoLogin { get; set; }
        public bool SecScanner { get; set; }
        public bool DocScanner { get; set; }
        public IEnumerable<IEnumerable<string>> SearchLists { get; set; }
    }
}
