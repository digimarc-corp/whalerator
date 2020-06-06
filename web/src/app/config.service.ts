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

import { Injectable } from '@angular/core';
import { tap, catchError } from 'rxjs/operators';
import { Config } from './models/config';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';
import { Observable, of } from 'rxjs';
import { WebService, isError } from './web-service';
import { Theme } from './models/theme';
import { Sorts } from './models/sorts';

@Injectable({
  providedIn: 'root'
})
export class ConfigService extends WebService {

  private apiBase: string;
  public config: Config;
  public errorMessage: string;
  public isErrored = false;
  public version: any;
  public themes: Theme[];
  public currentTheme: Theme;

  constructor(private http: HttpClient) {
    super();
    this.apiBase = environment.serviceBaseUri;
    this.getConfig();
    this.getVersion();
  }

  getVersion() {
    this.http.get<any>('/assets/v.json').pipe(
      tap(v => console.log('got app version')),
      catchError(this.handleError<any>('getVersion'))
    ).subscribe(v => {
      if (isError<any>(v)) {
        this.isErrored = true;
        this.errorMessage = v.message;
      } else {
        this.version = v;
      }
    });
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
        this.config = new Config(c);
        if (this.config.themes && this.config.themes.length > 0) {
          const favTheme = this.config.themes.find(t => t.name === localStorage.getItem('theme'));
          const theme = favTheme ? favTheme : this.config.themes[0];
          this.setTheme(theme);
        }
      }
    });
  }

  resetSorts() {
    Object.keys(localStorage).filter(s => s.startsWith('sort_')).map(s => localStorage.removeItem(s));
  }

  getRepoSort(repo: string): [Sorts, boolean] {
    const sort = localStorage.getItem('sort_' + repo) as Sorts;
    const asc = localStorage.getItem('sort_asc_' + repo) ? true : false;

    return [sort ? sort : this.getDefaultSort(), asc];
  }

  setRepoSort(repo: string, sort: Sorts, ascending: boolean) {
    localStorage.setItem('sort_' + repo, sort);
    if (ascending) {
      localStorage.setItem('sort_asc_' + repo, 'true');
    } else {
      localStorage.removeItem('sort_asc_' + repo);
    }
  }

  getDefaultSort(): Sorts {
    const sort = localStorage.getItem('defaultSort') as Sorts;
    return sort ? sort : Sorts.Semver;
  }

  setDefaultSort(sort: Sorts) {
    localStorage.setItem('defaultSort', sort);
  }

  setTheme(theme: Theme) {
    if (theme && document.getElementById('usertheme')) {
      this.currentTheme = theme;
      localStorage.setItem('theme', theme.name);
      (<HTMLLinkElement>document.getElementById('usertheme')).href = theme.style;
    }
  }
}
