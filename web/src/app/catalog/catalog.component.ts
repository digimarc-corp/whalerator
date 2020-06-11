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
    private titleService: Title) { }

  public repos: Repository[];
  public repoError: { [repo: string]: string } = {};
  public repoWorking: { [repo: string]: string } = {};
  public errorMessage: string;

  pagedRepos: Repository[];

  get repoCount(): number {
    return this.repos ? this.repos.length : 0;
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
    this.catalogService.getRepos().subscribe(r => {
      if (isError(r)) {
        if (r.resultCode === 401) {
          this.sessionService.logout();
          this.router.navigate(['/login'], { queryParams: { requested: this.location.path() } });
        } else {
          this.errorMessage = 'There was an error fetching the repository catalog.';
          console.log(r.message);
        }
      } else {
        this.repos = r;
        this.route.queryParams.subscribe(p => {
          this.pageSize = isNaN(p['items']) ? this.pageSize : Number(p['items']);
          this.pageCurrent = isNaN(p['page']) ? this.pageCurrent : Number(p['page']);
        });
      }
    });
  }

  updatePage() {
    if (this.pageSize > 0) {
      const start = (this.pageCurrent - 1) * this.pageSize;
      const end = start + this.pageSize;

      this.pagedRepos = this.repos.slice(start, end);
    } else {
      this.pagedRepos = this.repos;
    }
    this.router.navigate([], { relativeTo: this.route, queryParams: { items: this.pageSize, page: this.pageCurrent, }, replaceUrl: true });
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
          }
        });
      }
    }
  }
}
