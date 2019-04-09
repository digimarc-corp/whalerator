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

import { Platform } from './platform';
import { Document } from './document';
import { History } from './history';
import { ScanResult } from './scanResult';

export class Image {
    constructor(obj?: any) {
        Object.assign(this, obj);
        this.history = (this.history || []).map(o => new History(o));
        this.documents = this.documents ? this.documents.map(o => new Document(o)) : null;
        this.platform = new Platform(this.platform);
        this.scanResult = this.scanResult ? new ScanResult(this.scanResult) : null;
    }

    public digest: String;
    public platform: Platform;
    public history: History[];
    public documents: Document[];
    public scanResult: ScanResult;
}
