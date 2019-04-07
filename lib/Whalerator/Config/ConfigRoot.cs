﻿/*
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
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.Config
{
    public class ConfigRoot
    {
        public CacheConfig Cache { get; set; } = new CacheConfig();
        

        public SearchConfig Search { get; set; } = new SearchConfig();

        public SecurityConfig Security { get; set; } = new SecurityConfig();


        public ScannerConfig Scanner { get; set; } = new ScannerConfig();

        public CatalogConfig Catalog { get; set; } = new CatalogConfig();
    }
}