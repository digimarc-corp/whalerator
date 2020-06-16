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

import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { CatalogService } from '../catalog.service';
import { Repository } from '../models/repository';
import { isError } from '../web-service';
import { Title } from '@angular/platform-browser';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { ConfigService } from '../config.service';
import { MarkdownComponent } from 'ngx-markdown';
import { CatalogSort } from '../sorts/catalog-sort';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.scss']
})
export class CatalogComponent implements OnInit {

  constructor(public sessionService: SessionService,
    private configService: ConfigService,
    private catalogService: CatalogService,
    private location: Location,
    private router: Router,
    private route: ActivatedRoute,
    private titleService: Title) {
    route.queryParams.subscribe(p => this.checkParams(p));
  }

  public repos: Repository[];
  public paths: string[];
  public items: Array<Repository | string> = [];
  public repoError: { [repo: string]: string } = {};
  public repoWorking: { [repo: string]: string } = {};
  public errorMessage: string;

  public banner: string;

  _folder: string;
  get folder(): string {
    return this._folder;
  }
  set folder(value: string) {
    this._folder = value;
    this.updateItems();
  }

  get collapse(): boolean {
    return this.configService.collapseCatalog;
  }
  set collapse(value: boolean) {
    this.configService.collapseCatalog = value;
    this.updateItems();
    this.updatePage();
  }

  pagedItems: Array<Repository | string>;

  get repoCount(): number {
    return this.items ? this.items.length : 0;
  }

  _pageCurrent = 1;
  get pageCurrent(): number {
    return this._pageCurrent;
  }
  set pageCurrent(value: number) {
    this._pageCurrent = value;
    this.updatePage();
  }

  get pageSize(): number {
    const size = this.configService.pagerSize;
    return size ? size : 25;
  }
  set pageSize(value: number) {
    this.configService.pagerSize = value;
    if (value > 1) {
      const currentItem = (this.pageCurrent - 1) * this.pageSize;
      this.pageCurrent = Math.floor(currentItem / this.pageSize) + 1;
    } else {
      this.pageCurrent = 1;
    }
  }

  get pageCount(): number {
    return Math.max(1, Math.floor(this.repoCount / this.pageSize) + 1);
  }

  ngOnInit() {
    this.titleService.setTitle(this.sessionService.activeRegistry + ' - Catalog');
    this.catalogService.getBanner().subscribe(b => {
      if (isError(b)) {
        // do something
      } else {
        this.banner = b;
      }
    });
    this.catalogService.getRepos().subscribe(repos => {
      if (isError(repos)) {
        if (repos.resultCode === 401) {
          this.sessionService.logout();
          this.router.navigate(['/login'], { queryParams: { requested: this.location.path() } });
        } else {
          this.errorMessage = 'There was an error fetching the repository catalog.';
          console.log(repos.message);
        }
      } else {
        this.repos = repos;
        const paths = this.repos
          .map(r => r.name.split('/'))
          .filter(r => r.length > 1)
          .map(r => r.slice(0, r.length - 1).join('/'));
        this.paths = Array.from(new Set(paths));

        this.updateItems();

        this.route.queryParams.subscribe(p => this.checkParams(p));
      }
    });
  }

  private checkParams(p) {
    this.folder = p['path'];
    this.pageSize = isNaN(p['items']) ? this.pageSize : Number(p['items']);

    // pageCurrent setter triggers updates that could update the route, so it must be set last
    this.pageCurrent = isNaN(p['page']) ? this.pageCurrent : Number(p['page']);
  }

  path(item: string) {
    return this.folder ? this.folder + '/' + item : item;
  }

  childCount(item: Repository | string): number {
    if (this.isRepo(item)) {
      return item.tags;
    } else {
      const path = this.path(item);
      return this.repos.filter(r => r.name.startsWith(path)).map(r => r.tags).reduce((t, c) => t + c);
    }
  }

  private updateItems() {
    if (this.repos) {
      if (this.collapse) {
        let paths: string[];
        let repos: Repository[];
        if (this.folder) {
          paths = this.paths.filter(a => a !== this.folder && a.startsWith(this.folder + '/'));
          repos = this.repos.filter(a => a.name.startsWith(this.folder + '/'));
        } else {
          paths = this.paths;
          repos = this.repos;
        }

        // further filter path list to those that aren't sub-paths of another item
        const topPaths = paths.filter(a => !paths.some(b => a !== b && a.startsWith(b + '/')));

        const items = repos.filter(r => !topPaths.some(p => r.name.startsWith(p + '/')));
        this.items = [].concat(items).concat(topPaths.map(p => p.replace(this.folder + '/', '')));
        this.items.sort(CatalogSort.sort);
      } else {
        this._folder = null;
        this.items = this.repos.sort(CatalogSort.sort);
      }
    }
  }

  isRepo(item: Repository | string): item is Repository {
    return typeof item !== 'string';
  }

  breadCrumbs(): [string, string][] {
    const crumbs: [string, string][] = [];
    if (this.folder && this.paths) {
      const parents = this.paths.filter(p => p === this.folder || this.folder.startsWith(p + '/')).sort();
      let i: number;
      for (i = 0; i < parents.length; i++) {
        const path = parents[i];
        const label = i > 0 ? path.replace(parents[i - 1] + '/', '') : path;
        crumbs.push([path, label]);
      }
    }

    return crumbs;
  }

  updatePage() {
    if (this.pageCurrent > this.pageCount) {
      this._pageCurrent = this.pageCount;
    }

    if (this.pageSize > 0) {
      const start = (this.pageCurrent - 1) * this.pageSize;
      const end = start + this.pageSize;

      this.pagedItems = this.items.slice(start, end);
    } else {
      this.pagedItems = this.items;
    }
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { items: this.pageSize, page: this.pageCurrent, path: this.folder, },
      replaceUrl: true
    });
  }

  pageHome() {
    this.pageCurrent = 1;
  }

  pageBack() {
    if (this.pageCurrent > 1) {
      this.pageCurrent--;
    }
  }

  pageNext() {
    if (this.pageCurrent < this.pageCount) {
      this.pageCurrent++;
    }
  }

  pageEnd() {
    this.pageCurrent = this.pageCount;
  }

  delete(repo: Repository) {
    if (repo.canDelete) {
      const name = repo.name;
      if (!this.repoWorking[name]) {
        if (confirm(`Delete all images and tags in repository ${repo.name}?`)) {
          this.repoWorking[name] = 'Deleting';
          this.catalogService.deleteRepo(name).subscribe((e) => {
            if (isError(e)) {
              if (e.resultCode === 405) {
                this.repoError[name] = e.error;
              } else {
                this.repoError[name] = e.message;
              }
            } else {
              this.repos = this.repos.filter(r => r.name !== repo.name);
              delete this.repoWorking[name];
              this.updateItems();
              this.updatePage();
            }
          });
        }
      }
    }
  }
}
