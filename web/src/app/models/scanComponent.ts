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
import { Vulnerability } from "./vulnerability";
import { Severity } from "./severity";


export class ScanComponent {
    constructor(obj?: any) {
        Object.assign(this, obj);
        this.vulnerabilities = (this.vulnerabilities || []).map(v => new Vulnerability(v)).sort((a, b) => b.severity - a.severity);
    }

    public name: String;
    public addedBy: String;
    public version: String;
    public namespaceName: number;
    public vulnerabilities: Vulnerability[];

    public get maxSeverity(): number {
        return this.vulnerabilities.length == 0 ? 0 : Math.max.apply(Math, this.vulnerabilities.map(v => v.severity));
    }
}
