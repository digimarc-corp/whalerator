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

import { Component, OnInit, Version, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { ImageSet } from '../models/image-set';
import { SimplifiedSort } from '../sorts/simplified-sort';
import { Platform } from '../models/platform';
import { Image } from '../models/image';
import { isError } from '../web-service';
import { Permissions } from '../models/permissions';
import { ServiceError } from '../service-error';
import { Title } from '@angular/platform-browser';
import { SessionService } from '../session.service';
import { ConfigService } from '../config.service';
import { SortOrder } from '../models/sort-order';
import { SemverSort } from '../sorts/semver-sort';

@Component({
  selector: 'app-repository',
  templateUrl: './repository.component.html',
  styleUrls: ['./repository.component.scss']
})
export class RepositoryComponent implements OnInit {

  constructor(private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private catalog: CatalogService,
    private sessionService: SessionService,
    private configService: ConfigService,
    private changeDetector: ChangeDetectorRef,
    private titleService: Title) { }

  public name: string;

  requestedTag: string;
  loadRequested = false;

  public tags: string[];
  public filteredTags: string[];
  public selectedTag: string;
  public selectedImageSet: ImageSet;
  public selectedPlatform: Platform;
  public selectedImage: Image;
  public readme: string;

  private _sort = SortOrder.Semver;
  public get sort() {
    return this._sort;
  }
  public set sort(value: string) {
    this._sort = value as SortOrder;
    this.sortTags();
    this.configService.setRepoSort(this.name, this._sort, this.sortAscending);
  }
  public sortOptions = Object.keys(SortOrder);
  public sortAscending = false;

  public showCopyMsg: Boolean;

  // maps image digest strings to ImageSet objects
  public images: { [id: string]: ImageSet } = {};
  // maps tag strings to image digest strings
  public tagMap: { [tag: string]: string } = {};
  // maps tag strings to error messages
  public errorMap: { [tag: string]: string } = {};

  public permissions: Permissions;
  public get canDelete(): boolean {
    return this.permissions >= Permissions.Admin;
  }

  public errorMessage: string[] = [];

  // provide lookup for enum strings
  public permissionsType: typeof Permissions = Permissions;

  ngOnInit() {
    this.route.queryParams.subscribe(p => {
      const snapshot = this.route.snapshot.children[0];
      this.name = snapshot ? snapshot.url.join('/') : 'unknown';
      this.requestedTag = p['tag'];
      this.titleService.setTitle(this.sessionService.sessionLabel + '/' + this.name);
      this.getRepo();
    });
  }

  set searchQuery(search: string) {
    if (search) {
      this.filteredTags = this.tags.filter(t => t.includes(search));
    } else {
      this.filteredTags = null;
    }
  }

  toggleSortOrder() {
    this.sortAscending = !this.sortAscending;
    this.sortTags();
    this.configService.setRepoSort(this.name, this._sort, this.sortAscending);
  }

  sortTags() {
    if (this.tags) { this.applySort(this.tags); }
    if (this.filteredTags) { this.applySort(this.filteredTags); }
    if (this.selectedImageSet) { this.applySort(this.selectedImageSet.tags); }
  }

  applySort(tags: string[]) {
    switch (this._sort) {
      case SortOrder.Simplified:
        tags.sort(SimplifiedSort.sort);
        break;
      case SortOrder.Semver:
        tags.sort(SemverSort.sort);
        break;
      case SortOrder.Alpha:
        tags.sort();
        break;
    }
    if (this.sortAscending) {
      tags.reverse();
    }
  }

  delete(imageSet: ImageSet) {
    if (confirm('Delete this image and all related tags?')) {
      this.catalog.deleteImageSet(this.name, this.selectedImageSet.setDigest).subscribe(e => {
        if (isError(e)) {
          this.showError(e);
        } else {
          const nextImage = Object.values(this.images).find(i => i.setDigest !== imageSet.setDigest);
          if (nextImage) {
            /* this should work without forcing a reload, but for now we're being lazy about it
            imageSet.tags.forEach(t => {
              delete this.tagMap[t as string];
              delete this.tags[t as string];
            });
            delete this.images[imageSet.setDigest as string];
            this.selectTag(nextImage.tags[0]); */
            this.router.navigate([], { relativeTo: this.route, queryParams: { tag: nextImage.tags[0] }, replaceUrl: true })
              .then(() => location.reload());
          } else {
            this.router.navigate(['/']);
          }
        }
      });
    }
  }

  get fullPath(): string {
    return `${this.sessionService.registryPath}${this.name}:${this.selectedTag}`;
  }

  copyPath() {
    // @ts-ignore: the Clipboard API is new and shakily supported. Verified working in latest Chrome & Firefox, not in Safari
    navigator.clipboard.writeText(this.fullPath);

    this.showCopyMsg = true;
    setTimeout(() => { this.showCopyMsg = false; }, 3000);
  }

  selectPlatform(platform: Platform) {
    this.selectedPlatform = platform;
    this.selectedImage = this.selectedImageSet.images.find(i => i.platform.label === platform.label);
  }

  selectTag(tag: string) {
    this.selectedTag = tag;
    this.selectedImageSet = this.images[this.tagMap[tag]];
    this.applySort(this.selectedImageSet.tags);
    if (this.selectedImageSet) {
      if (!this.selectedPlatform || !this.selectedImageSet.platforms.some(p => p.label === this.selectedPlatform.label)) {
        this.selectedPlatform = this.selectedImageSet.images[0].platform;
      }
      this.selectedImage = this.selectedImageSet.images.find(i => i.platform.label === this.selectedPlatform.label);
    }
    this.router.navigate([], { relativeTo: this.route, queryParams: { tag: tag }, replaceUrl: true });
  }

  getRepo(): void {
    if (this.loadRequested) {
      return;
    } else {
      this.loadRequested = true;
      this.catalog.getTags(this.name).subscribe(tagset => {
        if (isError(tagset)) {
          this.showError(tagset);
        } else {
          this.permissions = tagset.permissions;
          this.tags = tagset.tags;
          this.sortTags();
          this.selectedTag = this.requestedTag || this.tags[0];
          this.getFirstImage(this.selectedTag, () => {
            this.tags.filter(t => t !== this.selectedTag).forEach(t => this.getImage(t));
          });
        }
      });
      const sort = this.configService.getRepoSort(this.name);
      this.sortAscending = sort[1];
      this.sort = sort[0];
    }
  }

  getFirstImage(tag: string, next: () => void) {
    this.getImage(tag, () => {
      if (next) { next(); }
    });
  }

  showError(error: ServiceError, next?: () => void) {
    if (error.resultCode === 401) {
      this.router.navigate(['/login'], { queryParams: { requested: this.location.path() } });
    } else if (error.resultCode === 405) {
      this.errorMessage.push(error.error);
    } else {
      this.errorMessage.push('There was an error while fetching repository information: ' + error.message);
      if (next) { next(); }
    }
  }

  showNotFound(tag: string, next?: () => void) {
    this.errorMessage.push(`Tag '${tag}' not found.`);
    if (next) { next(); }
  }

  // this is where the magic happens
  getImage(tag: string, next?: () => void) {
    // request the digest associated with this tag
    this.catalog.getImageSetDigest(this.name, tag).subscribe(digest => {
      if (isError(digest)) {
        if (digest.resultCode = 404) {
          this.showNotFound(tag, next);
        } else {
          this.showError(digest, next);
        }
      } else {
        // if we've already loaded an imageset with this digest, just make the map entry
        if (this.images[digest]) {
          this.mapTag(digest, tag);
          if (next) { next(); }
        } else {
          // fetch the actual imageset
          this.catalog.getImageSet(this.name, tag).subscribe(i => {
            if (isError(i)) {
              this.errorMap[tag] = i.error;
            } else if (!this.images[digest]) {
              // this request may have been duplicated since it was sent, so recheck the set of fetched images.
              // throwing away the extra information is cheaper than implementing locking or eliminating parallelism here.
              const setDigest = i.setDigest;
              i.tags = [tag];
              this.images[setDigest] = i;
              // if this tag is currently selected, set the active image and let the document view start loading details
              if (tag === this.selectedTag) {
                this.selectedImageSet = this.images[setDigest];
                this.selectedImage = this.images[setDigest].images[0];
                this.selectedPlatform = this.images[setDigest].images[0].platform;
              }
            }
            this.mapTag(digest, tag);
            if (next) {
              next();
            }
          });
        }
      }
    });
  }

  private mapTag(digest: string, tag: string) {
    if (this.images[digest] && !this.images[digest].tags.some(t => t === tag)) {
      this.images[digest].tags.push(tag);
      this.applySort(this.images[digest].tags);
    }
    this.tagMap[tag] = digest;
  }
}
