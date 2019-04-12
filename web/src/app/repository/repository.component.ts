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
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { ImageSet } from '../models/imageSet';
import { VersionSort } from '../version-sort';
import { Platform } from '../models/platform';
import { Image } from '../models/image';
import { isError } from '../web-service';
import { Permissions } from '../models/permissions';
import { ServiceError } from '../service-error';
import { Title } from '@angular/platform-browser';
import { SessionService } from '../session.service';
import { ConfigService } from '../config.service';

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
    private titleService: Title) { }

  public name: String;

  requestedTag: String;
  loadRequested = false;

  public tags: String[];
  public selectedTag: String;
  public selectedImageSet: ImageSet;
  public selectedPlatform: Platform;
  public selectedImage: Image;
  public readme: String;

  public showCopyMsg: Boolean;

  public images: { [id: string]: ImageSet } = {};
  public tagMap: { [tag: string]: ImageSet } = {};

  public permissions: Permissions;
  public get canDelete(): Boolean {
    return this.permissions >= Permissions.Admin;
  }

  public errorMessage: String[] = [];

  // provide lookup for enum strings
  public permissionsType: typeof Permissions = Permissions;

  ngOnInit() {
    this.route.queryParams.subscribe(p => {
      this.name = this.route.snapshot.children[0].url.join('/');
      this.requestedTag = p['tag'];
      this.titleService.setTitle(this.sessionService.activeRegistry + '/' + this.name.toString());
      this.getRepo();
    });
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

  get fullPath(): String {
    return `${this.sessionService.registryPath}${this.name}:${this.selectedTag}`;
  }

  copyPath() {
    // the Clipboard API is new and shakily supported. Verified working in latest Chrome & Firefox, not in Safari
    navigator.clipboard.writeText(this.fullPath.toString());

    this.showCopyMsg = true;
    setTimeout(() => { this.showCopyMsg = false; }, 3000);
  }

  selectPlatform(platform: Platform) {
    this.selectedPlatform = platform;
    this.selectedImage = this.selectedImageSet.images.find(i => i.platform.label === platform.label);
  }


  selectTag(tag: String) {
    this.selectedTag = tag;
    this.selectedImageSet = this.tagMap[tag.toString()];
    if (!this.selectedImageSet.platforms.some(p => p.label === this.selectedPlatform.label)) {
      this.selectedPlatform = this.selectedImageSet.images[0].platform;
      console.log('Setting platform ' + this.selectedPlatform.label);
    }
    this.selectedImage = this.selectedImageSet.images.find(i => i.platform.label === this.selectedPlatform.label);

    this.router.navigate([], { relativeTo: this.route, queryParams: { tag: tag }, replaceUrl: true });
  }

  getRepo(): void {
    if (this.loadRequested) {
      console.log('tag load already requested');
    } else {
      this.loadRequested = true;
      this.catalog.getTags(this.name).subscribe(tagset => {
        if (isError(tagset)) {
          this.showError(tagset);
        } else {
          this.permissions = tagset.permissions;
          this.tags = tagset.tags.sort(VersionSort.sort);
          this.selectedTag = this.requestedTag || this.tags[0];
          this.getFirstImage(this.selectedTag, () => {
            this.tags.filter(t => t !== this.selectedTag).forEach(t => this.getImage(t));
          });
        }
      });
    }
  }

  getFirstImage(tag: String, next: () => void) {
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

  showNotFound(tag: String, next?: () => void) {
    this.errorMessage.push(`Tag '${tag}' not found.`);
    if (next) { next(); }
  }

  // this is where the magic happens
  getImage(tag: String, next?: () => void) {
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
        if (this.images[digest.toString()]) {
          this.mapTag(digest, tag);
          if (next) { next(); }
        } else {
          // fetch the actual imageset
          this.catalog.getImageSet(this.name, tag).subscribe(i => {
            if (isError(i)) {
              this.showError(i, next);
            } else {
              // this request may have been duplicated since it was sent, so recheck the set of fetched images.
              // throwing away the extra information is cheaper than implementing locking or eliminating parallelism here.
              if (this.images[digest.toString()]) {
                this.mapTag(digest, tag);
              } else {
                const setDigest = i.setDigest.toString();
                i.tags = [tag];
                this.images[setDigest] = i;
                this.tagMap[tag.toString()] = this.images[setDigest];
                // if this tag is currently selected, set the active image and start loading documents
                if (tag === this.selectedTag) {
                  this.selectedImageSet = this.images[setDigest];
                  this.selectedImage = this.images[setDigest].images[0];
                  this.selectedPlatform = this.images[setDigest].images[0].platform;
                  // this.getDocuments(this.selectedImageSet, this.selectedImageSet.platforms[0], next);
                }
                if (next) {
                  next();
                }
              }
            }
          });
        }
      }
    });
  }

  private mapTag(digest: String, tag: String) {
    this.images[digest.toString()].tags.push(tag);
    this.tagMap[tag.toString()] = this.images[digest.toString()];
  }
  /*
    getDocuments(imageSet: ImageSet, platform: Platform, next?: () => void) {
      const digest = this.getDigestFor(imageSet, platform);
      const filename = 'readme.md';
      this.catalog.getFile(this.name, digest, filename).subscribe(r => {
        if (this.selectedImageSet === this.images[digest.toString()]) {
          this.readme = r.toString();
          if (next) { next(); }
        } else {
          console.log(`Discarded stale request for ${filename}`);
        }
      });
    }*/
}
