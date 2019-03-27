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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Whalerator.Client;

namespace Whalerator.Scanner
{
    public class ScanResult
    {
        public static ScanResult FromClair(ClairLayerResult result)
        {
            var vComponents = result.Layer.Features.Where(f => f.Vulnerabilities != null).ToList();
            var vulns = vComponents.SelectMany(c => c.Vulnerabilities).ToList();

            return new ScanResult
            {
                HighSeverityCount = vulns.Where(v => v.Severity >= Severity.High).Count(),
                MediumSeverityCount = vulns.Where(v => v.Severity == Severity.Medium).Count(),
                LowSeverityCount = vulns.Where(v => v.Severity <= Severity.Low).Count(),
                MaxSeverity = vulns.Max(v => v.Severity),
                TotalComponentsCount = result.Layer.Features.Count(),
                TotalVulnerabilitiesCount = vulns.Count(),
                VulnerableComponentCount = vComponents.Count(),
                VulnerableComponents = vComponents,
            };
        }

        public List<Component> VulnerableComponents { get; set; }
        public int VulnerableComponentCount { get; set; }
        public int TotalVulnerabilitiesCount { get; set; }
        public int TotalComponentsCount { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Severity MaxSeverity { get; set; }

        // We are simplifying, and anything lower than low or higher than high is just counted as low or high
        public int LowSeverityCount { get; set; }
        public int MediumSeverityCount { get; set; }
        public int HighSeverityCount { get; set; }
    }
}
