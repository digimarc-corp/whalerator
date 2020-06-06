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

import { Component, OnInit, Input } from '@angular/core';
import { ScanResult } from '../models/scanResult';
import { TypeofExpr } from '@angular/compiler';
import { Severity } from '../models/severity';

@Component({
  selector: 'app-scan-result',
  templateUrl: './scan-result.component.html',
  styleUrls: ['./scan-result.component.scss']
})
export class ScanResultComponent implements OnInit {
  @Input()
  set scan(scan: ScanResult) {
    this._scan = scan;
  }

  get scan() {
    return this._scan;
  }

  // provide lookup for enum strings
  public severityType: typeof Severity = Severity;

  private _scan: ScanResult;

  constructor() { }

  ngOnInit() {
  }
}
