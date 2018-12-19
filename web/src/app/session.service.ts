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
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Token } from './token';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { TokenRequest } from './token-request';
import { environment } from '../environments/environment';
import { WebService } from './web-service';
import { ServiceError } from './service-error';
import { JwtHelperService } from '@auth0/angular-jwt';


const httpOptions = {
  headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

const sessionKey = 'sessionToken';

@Injectable({ providedIn: 'root' })
export class SessionService extends WebService {

  constructor(private http: HttpClient) {
    super();
    this.setSession(localStorage.getItem(sessionKey) ? localStorage.getItem(sessionKey) : sessionStorage.getItem(sessionKey));
  }

  sessionToken: String;
  activeRegistry: String;
  activeUser: String;

  whaleratorUrl = environment.serviceBaseUri;

  private setSession(token: String) {
    this.sessionToken = token;
    if (token) {
      const helper = new JwtHelperService();
      const credential = helper.decodeToken(token.toString());
      this.activeRegistry = credential.Reg || 'Unknown Registry';
      this.activeUser = credential.Usr || 'Unknown User';
    } else {
      this.activeRegistry = null;
      this.activeUser = null;
    }
  }

  logout(): void {
    sessionStorage.removeItem(sessionKey);
    localStorage.removeItem(sessionKey);
    this.sessionToken = null;
  }

  login(username: String, password: String, registry: String, remember: Boolean): Observable<Token | ServiceError> {
    const tokenRequest = new TokenRequest();
    tokenRequest.username = username;
    tokenRequest.password = password;
    tokenRequest.registry = registry;

    const endpoint = this.whaleratorUrl + '/token';

    return this.http.post<Token>(endpoint, tokenRequest, httpOptions).pipe(
      tap((token: Token) => {
        this.setSession(token.token);
        sessionStorage.setItem(sessionKey, token.token.toString());
        if (remember) { localStorage.setItem(sessionKey, token.token.toString()); }
      }),
      catchError(this.handleError<Token>('getToken'))
    );
  }
}
