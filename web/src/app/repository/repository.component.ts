import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PathLocationStrategy, LocationStrategy } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { ImageSet } from '../models/imageSet';
import { VersionSort } from '../version-sort';
import { Platform } from '../models/platform';
import { Image } from '../models/image';
import { isError } from '../web-service';
import { ServiceError } from '../service-error';
import { Title } from '@angular/platform-browser';
import { SessionService } from '../session.service';

@Component({
  selector: 'app-repository',
  templateUrl: './repository.component.html',
  styleUrls: ['./repository.component.css']
})
export class RepositoryComponent implements OnInit {

  public name: String;

  requestedTag: String;
  loadRequested = false;

  public tags: String[];
  public selectedTag: String;
  public selectedImageSet: ImageSet;
  public readme: String;

  public images: { [id: string]: ImageSet } = {};
  public tagMap: { [tag: string]: ImageSet } = {};

  public errorMessage: String[] = [];

  private objectKeys = Object.keys;

  constructor(private route: ActivatedRoute,
    private router: Router,
    private catalog: CatalogService,
    private sessionService: SessionService,
    private titleService: Title) { }

  ngOnInit() {
    this.route.queryParams.subscribe(p => {
      this.name = this.route.snapshot.children[0].url.join('/');
      this.requestedTag = p['tag'];
      this.titleService.setTitle(this.sessionService.activeRegistry + '/' + this.name.toString());
      this.getRepo();
    });
  }

  getDigestFor(imageSet: ImageSet, platform: Platform): String {
    return imageSet.images.find(i => i.platform.architecture === platform.architecture && i.platform.os === platform.os).digest;
  }

  onSelect(tag: String) {
    this.selectedTag = tag;
    this.selectedImageSet = this.tagMap[tag.toString()];
    this.router.navigate([], { relativeTo: this.route, queryParams: { tag: tag }, replaceUrl: true });
    this.getReadme(this.selectedImageSet, this.selectedImageSet.platforms[0]);
  }

  getRepo(): void {
    if (this.loadRequested) {
      console.log('tag load already requested');
    } else {
      this.loadRequested = true;
      this.catalog.getTags(this.name).subscribe(tags => {
        if (isError(tags)) {
          this.showError(tags);
        } else {
          this.tags = tags.sort(VersionSort.sort);
          this.selectedTag = this.requestedTag || this.tags[0];
          this.getFirstImage(this.selectedTag, () => {
            tags.filter(t => t !== this.selectedTag)
              // .sort((a, b) => Math.floor(Math.random() * 2) - 1)
              .forEach(t => this.getImage(t));
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
    this.errorMessage.push('There was an error while fetching repository information: ' + error.message);
    if (next) { next(); }
  }

  showNotFound(tag: String, next?: () => void) {
    this.errorMessage.push(`Tag '${tag}' not found.`);
    if (next) { next(); }
  }

  getImage(tag: String, next?: () => void) {
    this.catalog.getImageSetDigest(this.name, tag).subscribe(digest => {
      if (isError(digest)) {
        if (digest.resultCode = 404) {
          this.showNotFound(tag, next);
        } else {
          this.showError(digest, next);
        }
      } else {
        if (this.images[digest.toString()]) {
          this.mapTag(digest, tag);
          if (next) { next(); }
        } else {
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
                if (tag === this.selectedTag) {
                  this.selectedImageSet = this.images[setDigest];
                  this.getReadme(this.selectedImageSet, this.selectedImageSet.platforms[0], next);
                } else {
                  if (next) {
                    next();
                  }
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

  getReadme(imageSet: ImageSet, platform: Platform, next?: () => void) {
    this.readme = 'Searching for documentation.';
    const digest = this.getDigestFor(imageSet, platform);
    this.catalog.getFile(this.name, digest, 'readme.md').subscribe(r => {
      this.readme = r.toString();
      if (next) { next(); }
    });
  }
}
