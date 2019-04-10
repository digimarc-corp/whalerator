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
import { Router } from '@angular/router';
import { Location } from '@angular/common';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.scss']
})
export class CatalogComponent implements OnInit {

  public repos: Repository[];
  public repoError: { [repo: string]: string } = {};
  public errorMessage: String;

  constructor(private sessionService: SessionService,
    private catalogService: CatalogService,
    private location: Location,
    private router: Router,
    private titleService: Title) { }

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
      }
    });
  }

  delete(repo: Repository) {
    if (confirm(`Delete all images and tags in repository ${repo.name}?`)) {
      // fire and forget delete request
      this.catalogService.deleteRepo(repo.name).subscribe((e) => {
        if (isError(e)) {
          this.repoError[repo.name.toString()] = e.message.toString();
        } else {
          this.repos = this.repos.filter(r => r.name !== repo.name);
        }
      });
    }
  }
}
