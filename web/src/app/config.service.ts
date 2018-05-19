import { Injectable } from '@angular/core';
import { tap, catchError } from 'rxjs/operators';
import { Config } from './models/config';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Observable, of } from 'rxjs';
import { WebService, isError } from './web-service';

@Injectable({
  providedIn: 'root'
})
export class ConfigService extends WebService {

  private apiBase: String;
  public config: Config;
  public errorMessage: String;
  public isErrored = false;

  constructor(private http: HttpClient) {
    super();
    this.apiBase = environment.serviceBaseUri;
    this.getConfig();
  }

  getConfig() {
    const configUrl = this.apiBase + '/config';
    this.http.get<Config>(configUrl).pipe(
      tap(config => console.log('got service config')),
      catchError(this.handleError<Config>('getConfig'))
    ).subscribe(c => {
      if (isError<Config>(c)) {
        this.isErrored = true;
        this.errorMessage = c.message;
      } else {
        this.config = c;
      }
    });
  }
}
