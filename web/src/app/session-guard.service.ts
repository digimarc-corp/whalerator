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

import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { SessionService } from './session.service';
import { Location } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class SessionGuard implements CanActivate {
  constructor(private authService: SessionService, private router: Router, private location: Location) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.authService.sessionToken) {
      this.router.navigate(['/login'], { queryParams: { requested: this.location.path() } });
      return false;
    } else {
      return true;
    }
  }
}
