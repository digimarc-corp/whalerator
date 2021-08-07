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

import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { SemverSort } from './semver-sort';

describe('SemverSort', () => {

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({})
      .compileComponents();
  }));

  it('should sort list', () => {
    const unsorted = [
      'qa1.2.3',
      'alpha.23',
      '0',
      '1.2.3-a4',
      '1.2',
      'latest',
      'latest-rc',
      'v1.2.2',
      '1.2.3',
      'dev-9',
      'latest-beta',
      '1.2.3-rc3',
      '1',
      '0.2.3.4',
      '1.0.0',
    ];
    const sorted = [
      'latest',
      'latest-beta',
      'latest-rc',
      '1',
      '1.2',
      '1.2.3',
      'v1.2.2',
      '1.0.0',
      '0',
      '1.2.3-a4',
      '1.2.3-rc3',
      '0.2.3.4',
      'alpha.23',
      'dev-9',
      'qa1.2.3',
    ];

    unsorted.sort(SemverSort.sort);

    expect(unsorted).toEqual(sorted);
  });

});
