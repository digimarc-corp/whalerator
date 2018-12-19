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

import { Injectable, OnInit } from '@angular/core';
import { SessionService } from './session.service';
import { Observable, of } from 'rxjs';
import { Repository } from './models/repository';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { ImageSet } from './models/imageSet';
import { environment } from '../environments/environment';
import { Config } from './models/config';
import { WebService } from './web-service';
import { ServiceError } from './service-error';
import { History } from './models/history';


@Injectable({
  providedIn: 'root'
})
export class CatalogService extends WebService {

  private apiBase = environment.serviceBaseUri;

  public config: Config;

  constructor(private http: HttpClient,
    private sessionService: SessionService) { super(); }

  getRepos(): Observable<Repository[] | ServiceError> {
    const listUrl = this.apiBase + '/repositories/list';
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<Repository[]>(listUrl, { headers: headers }).pipe(
      tap(repos => console.log('got repo list')),
      catchError(this.handleError<Repository[]>('getRepos'))
    );
  }

  getTags(repo: String): Observable<String[] | ServiceError> {
    const tagsUrl = this.apiBase + `/repository/${repo}/tags/list`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<String[]>(tagsUrl, { headers: headers }).pipe(
      tap(repos => console.log('got tag list')),
      catchError(this.handleError<String[]>('getTags'))
    );
  }

  getImageSet(repo: String, tag: String): Observable<ImageSet | ServiceError> {
    const imageUrl = this.apiBase + `/repository/${repo}/tag/${tag}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<ImageSet>(imageUrl, { headers: headers }).pipe(
      tap(img => console.log('got image set')),
      tap(imgset => imgset.images.forEach(img => img.history = img.history.map(i => History.From(i)))),
      catchError(this.handleError<ImageSet>('getImage'))
    );
  }

  getImageSetDigest(repo: String, tag: String): Observable<String | ServiceError> {
    const imageUrl = this.apiBase + `/repository/${repo}/digest/${tag}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<String>(imageUrl, { headers: headers }).pipe(
      tap(repos => console.log('got image set')),
      catchError(this.handleError<String>('getImage'))
    );
  }

  getFile(repo: String, digest: String, path: String): Observable<Object> {
    const fileUrl = this.apiBase + `/repository/${repo}/file/${digest}?path=${path}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get(fileUrl, { headers: headers, responseType: 'text' }).pipe(
      tap(repos => console.log('got file contents')),
      catchError(this.handleError('getFile', 'No embedded documentation found'))
    );
  }

  private handleGetFileError(operation = 'operation', path: String, result?: Object) {
    return (error: any): Observable<Object> => {
      if (error.status === 404) {
        return of(`Could not find a file at \`/${path}\`.`);
      } else {
        console.error(error);
        return of(result);
      }
    };
  }
}
