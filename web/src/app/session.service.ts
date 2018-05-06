import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Token } from './token';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { TokenRequest } from './token-request';


const httpOptions = {
  headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

@Injectable({ providedIn: 'root' })
export class SessionService {

  constructor(private http: HttpClient) { }

  sessionToken: String;

  whaleratorUrl = 'http://localhost:16545/api';

  getToken(username: String, password: String, registry: String): Observable<Token> {
    const tokenRequest = new TokenRequest();
    tokenRequest.username = username;
    tokenRequest.password = password;
    tokenRequest.registry = registry;

    const endpoint = this.whaleratorUrl + '/token';
    const json = JSON.stringify(tokenRequest);

    return this.http.post<Token>(endpoint, tokenRequest, httpOptions).pipe(
      tap((token: Token) => this.sessionToken = token.token),
      catchError(this.handleError<Token>('getToken'))
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
