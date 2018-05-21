import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
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

  public tags: String[];
  public selectedTag: String;
  public selectedImageSet: ImageSet;
  public readme: String;

  public images: { [id: string]: ImageSet } = {};
  public tagMap: { [tag: string]: ImageSet } = {};

  public errorMessage: String;

  private objectKeys = Object.keys;

  constructor(private route: ActivatedRoute,
    private catalog: CatalogService,
    private sessionService: SessionService,
    private titleService: Title) { }

  ngOnInit() {
    this.name = this.route.snapshot.children[0].url.join('/');
    this.titleService.setTitle(this.sessionService.activeRegistry + '/' + this.name.toString());
    this.getRepo();
  }

  getDigestFor(imageSet: ImageSet, platform: Platform): String {
    return imageSet.images.find(i => i.platform.architecture === platform.architecture && i.platform.os === platform.os).digest;
  }

  onSelect(tag: String) {
    this.selectedTag = tag;
    this.selectedImageSet = this.tagMap[tag.toString()];
    this.getReadme(this.selectedImageSet, this.selectedImageSet.platforms[0]);
  }

  getRepo(): void {
    this.catalog.getTags(this.name).subscribe(tags => {
      if (isError(tags)) {
        this.showError(tags);
      } else {
        this.tags = tags.sort(VersionSort.sort);
        this.selectedTag = this.tags[0];
        this.getFirstImage(this.selectedTag, () => {
          tags.slice(1).forEach(t => this.getImage(t));
        });
      }
    });
  }

  getFirstImage(tag: String, next: () => void) {
    this.getImage(tag, () => {
      if (next) { next(); }
    });
  }

  showError(error: ServiceError) {
    this.errorMessage = 'There was an error while fetching repository information: ' + error.message;
  }

  getImage(tag: String, next?: () => void) {
    this.catalog.getImageSetDigest(this.name, tag).subscribe(digest => {
      if (isError(digest)) {
        this.showError(digest);
      } else {
        if (this.images[digest.toString()]) {
          this.images[digest.toString()].tags.push(tag);
          this.tagMap[tag.toString()] = this.images[digest.toString()];
          if (next) { next(); }
        } else {
          this.catalog.getImageSet(this.name, tag).subscribe(i => {
            if (isError(i)) {
              this.showError(i);
            } else {
              const setDigest = i.setDigest.toString();
              i.tags = [tag];
              this.images[setDigest] = i;
              this.tagMap[tag.toString()] = this.images[setDigest];
              if (tag === this.selectedTag) {
                this.selectedImageSet = this.images[setDigest];
                this.getReadme(this.selectedImageSet, this.selectedImageSet.platforms[0], next);
              } else {
                if (next) { next(); }
              }
            }
          });
        }
      }
    });
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
