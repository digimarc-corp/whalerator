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

export class FileListing {
    constructor(obj?: any) {
        Object.assign(this, obj);
    }

    public digest: string;
    public depth: string;
    public files: string[];

    public static From(obj: any): FileListing {
        const list = new FileListing();
        list.depth = obj.depth;
        list.digest = obj.digest;
        list.files = obj.files;
        return list;
    }
}
