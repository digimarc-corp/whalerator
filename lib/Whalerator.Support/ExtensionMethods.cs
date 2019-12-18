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
using System.Linq;
using System.Text;
using Whalerator.Scanners.Security;

namespace Whalerator.Support
{
    public static class ExtensionMethods
    {
        public static ScanResult ToScanResult(this ClairLayerResult result)
        {
            var vComponents = result.Layer.Features == null ? new List<Component>() : result.Layer.Features.Where(f => f.Vulnerabilities != null).ToList();
            var vulns = vComponents.SelectMany(c => c.Vulnerabilities).ToList();

            return new ScanResult
            {
                Status = ScanStatus.Succeeded,
                TotalComponents = result.Layer.Features?.Count() ?? 0,
                VulnerableComponents = vComponents,
            };
        }

    }
}
