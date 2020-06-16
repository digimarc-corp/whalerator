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
import { Image } from './image';

export class ImageSet {
    constructor(obj?: any) {
        Object.assign(this, obj);
        this.images = (this.images || []).map(o => new Image(o));
        this.platforms = (this.platforms || []).map(o => new Platform(o));
    }

    public platforms: Platform[];
    public date: Date;
    public setDigest: string;
    public tags: string[];
    public images: Image[];
    public banner: string;
}
