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

import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { SemverSort } from './semver-sort';
import { Repository } from '../models/repository';
import { CatalogSort } from './catalog-sort';

describe('CatalogSort', () => {

    beforeEach(async(() => {
        TestBed.configureTestingModule({})
            .compileComponents();
    }));

    it('should sort list', () => {
        const unsorted = [
            'test',
            'alpha',
            'test',
            new Repository({ name: 'baker' }),
            'foo/bar',
            '123',
            new Repository({ name: 'able' }),
            'joe',
            'xyz',
            new Repository({ name: 'charlie' }),
            'foo',
        ];
        const sorted = [
            '123',
            new Repository({ name: 'able' }),
            'alpha',
            new Repository({ name: 'baker' }),
            new Repository({ name: 'charlie' }),
            'foo',
            'foo/bar',
            'joe',
            'test',
            'test',
            'xyz',
        ];

        unsorted.sort(CatalogSort.sort);

        expect(unsorted).toEqual(sorted);
    });

});
