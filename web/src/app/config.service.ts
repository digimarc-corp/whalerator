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
import { SortOrder } from './models/sort-order';

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

  public get collapseCatalog(): boolean {
    const collapse = localStorage.getItem('collapseCatalog');
    return collapse ? collapse === 'true' : true;
  }
  public set collapseCatalog(value: boolean) {
    localStorage.setItem('collapseCatalog', value.toString());
  }

  public get pagerSize(): number {
    return Number(localStorage.getItem('pager'));
  }
  public set pagerSize(value: number) {
    localStorage.setItem('pager', value.toString());
  }

  constructor(private http: HttpClient) {
    super();
    let serviceUri = environment.serviceBaseUri;

    if (serviceUri.indexOf('http://') === 0 || serviceUri.indexOf('https://') === 0) {
      this.apiBase = serviceUri;
    } else {
      let baseUri = document.baseURI;
      if (!baseUri.endsWith('/')) { baseUri = baseUri + '/'; }
      this.apiBase = new URL(environment.serviceBaseUri, baseUri).href;
    }

    this.getConfig();
    this.getVersion();
  }

  getVersion() {
    const versionUrl = this.apiBase + '/assets/v.json';
    this.http.get<any>(versionUrl).pipe(
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

  getRepoSort(repo: string): [SortOrder, boolean] {
    const sort = localStorage.getItem('sort_' + repo) as SortOrder;
    const asc = localStorage.getItem('sort_asc_' + repo) ? true : false;

    return [sort ? sort : this.getDefaultSort(), asc];
  }

  setRepoSort(repo: string, sort: SortOrder, ascending: boolean) {
    localStorage.setItem('sort_' + repo, sort);
    if (ascending) {
      localStorage.setItem('sort_asc_' + repo, 'true');
    } else {
      localStorage.removeItem('sort_asc_' + repo);
    }
  }

  getDefaultSort(): SortOrder {
    const sort = localStorage.getItem('defaultSort') as SortOrder;
    return sort ? sort : SortOrder.Semver;
  }

  setDefaultSort(sort: SortOrder) {
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
