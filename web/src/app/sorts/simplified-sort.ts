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

export class SimplifiedSort {
    // Sorts string using simplified assumptions about tags and versions syntax. 'latest' will always come first if present.
    // Strings which look like 'v1.0' or '1.0' will sort ahead of non-version strings, and will use descending ordering.
    // Other strings will use ascending standard alpha sort. Case insensitive.
    public static sort(a: string, b: string): number {
        const version = /^[vV]?[0-9\.]+/;

        if (a.toLowerCase() === 'latest') { return -1; }
        if (b.toLowerCase() === 'latest') { return 1; }
        if (version.test(a) && !version.test(b)) { return -1; }
        if (version.test(b) && !version.test(a)) { return 1; }
        if (version.test(a) && version.test(b)) {
            return a.toLowerCase() === b.toLowerCase() ? 0 : a.toLowerCase() < b.toLowerCase() ? 1 : -1;
        } else {
            return a.toLowerCase() === b.toLowerCase() ? 0 : b.toLowerCase() < a.toLowerCase() ? 1 : -1;
        }
    }
}
