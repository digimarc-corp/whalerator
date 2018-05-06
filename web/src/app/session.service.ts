import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SessionService {

  constructor() { }

  sessionToken: String;

  getToken(username: String, password: String, registry: String) {
    this.sessionToken = `${ username }`;
  }
}
