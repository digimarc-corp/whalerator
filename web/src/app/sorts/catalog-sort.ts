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

import { Repository } from '../models/repository';

export class CatalogSort {

    /* Sorts an array of union types
     */
    public static sort(a: Repository | string, b: Repository | string): number {
        const a_str = typeof a === 'string';
        const b_str = typeof b === 'string';
        if (a_str && b_str) {
            return (a as string).localeCompare(b as string);
        } else if (a_str && !b_str) {
            return (a as string).localeCompare((b as Repository).name);
        } else if (!a_str && b_str) {
            return (a as Repository).name.localeCompare(b as string);
        } else {
            return (a as Repository).name.localeCompare((b as Repository).name);
        }
    }

}
