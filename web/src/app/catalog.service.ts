import { Injectable } from '@angular/core';
import { SessionService } from './session.service';
import { Observable, of } from 'rxjs';
import { Repository } from './repository';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';


@Injectable({
  providedIn: 'root'
})
export class CatalogService {

  private listUrl = 'http://localhost:16545/api/repositories/list';

  constructor(private http: HttpClient,
    private sessionService: SessionService) { }

  getRepos(): Observable<String[]> {
    let headers = new HttpHeaders();
    headers = headers.append('Authorization', `Bearer ${this.sessionService.sessionToken}`);
    return this.http.get<String[]>(this.listUrl, { headers: headers }).pipe(
      tap(repos => console.log('got repo list')),
      catchError(this.handleError<String[]>('getRepos'))
    );
  }

  /**
   * Handle Http operation that failed.
   * Let the app continue.
   * @param operation - name of the operation that failed
   * @param result - optional value to return as the observable result
   */
  private handleError<T> (operation = 'operation', result?: T) {
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
