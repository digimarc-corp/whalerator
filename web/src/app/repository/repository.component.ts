import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PathLocationStrategy, LocationStrategy } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { ImageSet } from '../models/imageSet';
import { VersionSort } from '../version-sort';
import { Platform } from '../models/platform';
import { Image } from '../models/image';

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

  private objectKeys = Object.keys;

  constructor(private route: ActivatedRoute, private catalog: CatalogService) { }

  ngOnInit() {
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
    this.name = this.route.snapshot.children[0].url.join('/');
    this.catalog.getTags(this.name).subscribe(tags => {
      this.tags = tags.sort(VersionSort.sort);
      this.selectedTag = this.tags[0];
      this.getFirstImage(this.selectedTag, () => {
        tags.slice(1).forEach(t => this.getImage(t));
      });
    });
  }

  getFirstImage(tag: String, next: () => void) {
    this.getImage(tag, () => {
      if (next) { next(); }
    });
  }

  getImage(tag: String, next?: () => void) {
    this.catalog.getImageSetDigest(this.name, tag).subscribe(digest => {
      if (this.images[digest.toString()]) {
        this.images[digest.toString()].tags.push(tag);
        this.tagMap[tag.toString()] = this.images[digest.toString()];
        if (next) { next(); }
      } else {
        this.catalog.getImageSet(this.name, tag).subscribe(i => {
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
        });
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
