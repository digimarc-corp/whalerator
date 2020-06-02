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
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { ImageSet } from './models/imageSet';
import { environment } from '../environments/environment';
import { Config } from './models/config';
import { WebService } from './web-service';
import { ServiceError } from './service-error';
import { History } from './models/history';
import { TestBed } from '@angular/core/testing';
import { ScanResult } from './models/scanResult';
import { TagSet } from './models/tagSet';
import { FileListing } from './models/fileListing';


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
      map(repos => repos.map(r => new Repository(r))),
      catchError(this.handleError<Repository[]>('getRepos'))
    );
  }

  deleteRepo(repo: string): Observable<Object | ServiceError> {
    const imageUrl = this.apiBase + `/repositories/${repo}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.delete(imageUrl, { headers: headers }).pipe(
      catchError(this.handleError('deleteImage'))
    );
  }

  getTags(repo: string): Observable<TagSet | ServiceError> {
    const tagsUrl = this.apiBase + `/repository/${repo}/tags/list`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<TagSet>(tagsUrl, { headers: headers }).pipe(
      tap(tags => console.log('got tag list')),
      map(tags => new TagSet(tags)),
      catchError(this.handleError<TagSet>('getTags'))
    );
  }

  getImageSet(repo: string, tag: string): Observable<ImageSet | ServiceError> {
    const imageUrl = this.apiBase + `/repository/${repo}/tag/${tag}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<ImageSet>(imageUrl, { headers: headers }).pipe(
      tap(img => console.log('got image set')),
      tap(imgset => imgset.images.forEach(img => img.history = img.history.map(i => History.From(i)))),
      map(i => new ImageSet(i)),
      catchError(this.handleError<ImageSet>('getImage'))
    );
  }

  deleteImageSet(repo: string, digest: string): Observable<Object | ServiceError> {
    const imageUrl = this.apiBase + `/repository/${repo}/digest/${digest}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.delete(imageUrl, { headers: headers }).pipe(
      catchError(this.handleError('deleteImage'))
    );
  }

  getImageSetDigest(repo: string, tag: string): Observable<string | ServiceError> {
    const imageUrl = this.apiBase + `/repository/${repo}/digest/${tag}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<string>(imageUrl, { headers: headers }).pipe(
      tap(repos => console.log('got image set')),
      catchError(this.handleError<string>('getImage'))
    );
  }

  getFileList(repo: string, digest: string, targets: string): Observable<FileListing[] | HttpResponse<FileListing[]> | ServiceError> {
    const filesUrl = this.apiBase + `/repository/${repo}/files/${digest}?targets=${targets}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<FileListing[]>(filesUrl, { headers: headers, observe: 'response' }).pipe(
      tap(list => console.log('got file listings')),
      catchError(this.handleError<FileListing[]>('getScan'))
    );
  }

  getFile(repo: string, digest: string, path: string): Observable<string | ServiceError | HttpResponse<string>> {
    const fileUrl = this.apiBase + `/repository/${repo}/file/${digest}?path=${path}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get(fileUrl, { headers: headers, responseType: 'text', observe: 'response' }).pipe(
      tap(repos => console.log('got file contents')),
      catchError(this.handleError('getFile', 'No embedded documentation found'))
    );
  }

  getScan(repo: string, digest: string): Observable<ScanResult | ServiceError> {
    const scanUrl = this.apiBase + `/repository/${repo}/sec/${digest}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<ScanResult>(scanUrl, { headers: headers }).pipe(
      tap(scan => scan.digest = digest),
      tap(scan => console.log('got scan results')),
      catchError(this.handleError<ScanResult>('getScan'))
    );
  }

  private handleGetFileError(operation = 'operation', path: string, result?: Object) {
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
