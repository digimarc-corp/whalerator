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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Whalerator.Config;

namespace Whalerator
{
    public class IntervalParser
    {
        public static bool TryParse(string str, out TimeSpan interval)
        {
            try
            {
                interval = Parse(str);
                return true;
            }
            catch
            {
                interval = default;
                return false;
            }
        }

        private static Regex interval = new Regex(@"^(?<value>[\d,\.]+)(?<unit>\D{0,2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static TimeSpan Parse(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException();
            }
            else if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException();
            }

            var match = interval.Match(str);
            if (match.Success)
            {
                var value = match.Groups["value"].Value;

                if (float.TryParse(value, out var interval))
                {
                    var unit = match.Groups["unit"].Value;
                    unit = string.IsNullOrEmpty(unit) ? "ms" : unit;

                    return unit switch
                    {
                        "ms" => TimeSpan.FromMilliseconds(interval),
                        "s" => TimeSpan.FromSeconds(interval),
                        "m" => TimeSpan.FromMinutes(interval),
                        "h" => TimeSpan.FromHours(interval),
                        "d" => TimeSpan.FromDays(interval),
                        "w" => TimeSpan.FromDays(interval * 7),
                        "y" => TimeSpan.FromDays(interval * 365),
                        _ => throw new ArgumentException($"Unexpected unit '{unit}'")
                    };
                }
                else
                {
                    throw new ArgumentException($"Could not parse {value} as a numeric value");
                }
            }
            else
            {
                throw new ArgumentException($"Could not parse {str} as a time interval.");
            }
        }
    }
}
