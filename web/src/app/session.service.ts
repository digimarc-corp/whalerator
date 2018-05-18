import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Token } from './token';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { TokenRequest } from './token-request';
import { environment } from '../environments/environment';
import { WebService } from './web-service';


const httpOptions = {
  headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

const sessionKey = 'sessionToken';

@Injectable({ providedIn: 'root' })
export class SessionService extends WebService {

  constructor(private http: HttpClient) {
    super();
    this.sessionToken = localStorage.getItem(sessionKey) ? localStorage.getItem(sessionKey) : sessionStorage.getItem(sessionKey);
  }

  sessionToken: String;

  whaleratorUrl = environment.serviceBaseUri;

  logout(): void {
    sessionStorage.removeItem(sessionKey);
    localStorage.removeItem(sessionKey);
    this.sessionToken = null;
  }

  login(username: String, password: String, registry: String, remember: Boolean): Observable<Token> {
    const tokenRequest = new TokenRequest();
    tokenRequest.username = username;
    tokenRequest.password = password;
    tokenRequest.registry = registry;

    const endpoint = this.whaleratorUrl + '/token';

    return this.http.post<Token>(endpoint, tokenRequest, httpOptions).pipe(
      tap((token: Token) => {
        this.sessionToken = token.token;
        sessionStorage.setItem(sessionKey, token.token.toString());
        if (remember) { localStorage.setItem(sessionKey, token.token.toString()); }
      }),
      catchError(this.handleError<Token>('getToken'))
    );
  }
}
