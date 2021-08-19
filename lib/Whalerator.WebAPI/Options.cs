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

using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class Options
    {
        [Option('x', "exit", Required = false, HelpText = "Exit after loading configuration and processing rescan, if requested.")]
        public bool Exit { get; set; }

        [Option('r', "rescan", Required = false, HelpText = "Enumerate all repositories, images, and tags in the configured registry, and submit for indexing and/or scanning.")]
        public bool Rescan { get; set; }

        [Option('b', "nobanner", Required = false, HelpText = "Skip printing startup banner.")]
        public bool NoBanner { get; set; }
    }
}
