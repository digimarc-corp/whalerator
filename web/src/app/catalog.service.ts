import { Injectable, OnInit } from '@angular/core';
import { SessionService } from './session.service';
import { Observable, of } from 'rxjs';
import { Repository } from './models/repository';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { ImageSet } from './models/imageSet';
import { environment } from '../environments/environment';
import { Config } from './models/config';


@Injectable({
  providedIn: 'root'
})
export class CatalogService {

  private apiBase = environment.serviceBaseUri;

  public config: Config;

  constructor(private http: HttpClient, private sessionService: SessionService) { }

  getRepos(): Observable<Repository[]> {
    const listUrl = this.apiBase + '/repositories/list';
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<Repository[]>(listUrl, { headers: headers }).pipe(
      tap(repos => console.log('got repo list')),
      catchError(this.handleError<Repository[]>('getRepos'))
    );
  }

  getTags(repo: String): Observable<String[]> {
    const tagsUrl = this.apiBase + `/repository/${repo}/tags/list`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<String[]>(tagsUrl, { headers: headers }).pipe(
      tap(repos => console.log('got tag list')),
      catchError(this.handleError<String[]>('getTags'))
    );
  }

  getImageSet(repo: String, tag: String): Observable<ImageSet> {
    const imageUrl = this.apiBase + `/repository/${repo}/tag/${tag}`;
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<ImageSet>(imageUrl, { headers: headers }).pipe(
      tap(repos => console.log('got image set')),
      catchError(this.handleError<ImageSet>('getImage'))
    );
  }

  getImageSetDigest(repo: String, tag: String): Observable<String> {
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

  /**
   * Handle Http operation that failed.
   * Let the app continue.
   * @param operation - name of the operation that failed
   * @param result - optional value to return as the observable result
   */
  private handleError<T>(operation = 'operation', result?: T) {
    return (error: any): Observable<T> => {

      // TODO: send the error to remote logging infrastructure
      console.error(error); // log to console instead

      // TODO: better job of transforming error for user consumption
      // this.log(`${operation} failed: ${error.message}`);

      // Let the app keep running by returning an empty result.
      return of(result as T);
    };
  }
}
