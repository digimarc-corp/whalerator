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

namespace Whalerator.Config
{
    public record Placeholder
    {
        /// <summary>
        /// Placeholder text to be presented within a login textbox.
        /// </summary>
        public string PlaceholderText { get; init; }

        /// <summary>
        /// If true, the placeholder text is set as an actual default value for the textbox.
        /// </summary>
        public bool IsDefault { get; init; }

        /// <summary>
        /// If true, the placeholder text cannot be changed.
        /// </summary>
        public bool IsReadonly { get; init; }

        /// <summary>
        /// If true, the textbox is hidden entirely.
        /// </summary>
        public bool IsHidden { get; init; }
    }
}
