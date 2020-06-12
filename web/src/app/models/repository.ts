import { Permissions } from "./permissions";

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

export class Repository {
    constructor(obj?: any) {
        Object.assign(this, obj);
        this.permissions = Permissions[obj.permissions as keyof typeof Permissions];
    }

    public name: string;
    public tags: number;
    public permissions: Permissions;

    public get shortName() {
        const parts = this.name.split('/');
        return parts[parts.length - 1];
    }

    public get canDelete(): boolean {
        return this.permissions >= Permissions.Admin;
    }
}
