/*
   Copyright 2021 Digimarc, Inc

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


using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.WebAPI
{
    public class AnsiLogOptions : ConsoleFormatterOptions
    {
        public const bool LogStackDefault = true;
        public const bool LogCategoryDefault = true;
        public const bool LogTimestampDefault = true;
        public const bool LogSingleLineDefault = false;

        /// <summary>
        /// Include stack trace information when outputting errors
        /// </summary>
        public bool LogStack { get; set; } = LogStackDefault;

        /// <summary>
        /// Include log source category name in output
        /// </summary>
        public bool LogCategory { get; set; } = LogCategoryDefault;

        /// <summary>
        /// Include log timestamp in output
        /// </summary>
        public bool LogTimestamp { get; set; } = LogTimestampDefault;

        /// <summary>
        /// Strip/escape newlines from log messages, including exceptions
        /// </summary>
        public bool LogSingleLine { get; set; } = LogSingleLineDefault;

        public void ConfigureWith(AnsiLogOptions options)
        {
            IncludeScopes = options.IncludeScopes;
            LogCategory = options.LogCategory;
            LogStack = options.LogStack;
            LogSingleLine = options.LogSingleLine;
        }
    }
}
