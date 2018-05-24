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
