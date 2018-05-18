import { Injectable } from '@angular/core';
import { tap, catchError } from 'rxjs/operators';
import { Config } from './models/config';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {

  private apiBase: String;
  public config: Config;

  constructor(private http: HttpClient) {
    this.apiBase = environment.serviceBaseUri;
    this.getConfig();
  }

  getConfig() {
    const configUrl = this.apiBase + '/config';
    this.http.get<Config>(configUrl).pipe(
      tap(config => console.log('got service config')),
      catchError(this.handleError<Config>('getConfig'))
    ).subscribe(c => {
      this.config = c;
    });
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
