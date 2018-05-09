import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PathLocationStrategy, LocationStrategy } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { ImageSet } from '../models/imageSet';
import { VersionSort } from '../version-sort';
import { Platform } from '../models/platform';

@Component({
  selector: 'app-repository',
  templateUrl: './repository.component.html',
  styleUrls: ['./repository.component.css']
})
export class RepositoryComponent implements OnInit {

  public name: String;

  public tags: String[];
  public selectedTag: String;
  public selectedImage: ImageSet;

  public images: { [id: string]: ImageSet } = { };
  public tagMap: { [tag: string]: ImageSet } = { };

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
    this.selectedImage = this.tagMap[tag.toString()];
  }

  getRepo(): void {
    this.name = this.route.snapshot.children[0].url.join('/');
    this.catalog.getTags(this.name).subscribe(tags => {
      this.tags = tags.sort(VersionSort.sort);
      if (this.tags[0].toLowerCase() === 'latest') { this.selectedTag = this.tags[0]; }
      this.tags.forEach(t => this.getImage(t));
    });
  }

  getImage(tag: String): void {
    this.catalog.getImage(this.name, tag).subscribe(i => {
      const digest = i.setDigest.toString();
      if (!this.images[digest]) {
        i.tags = [tag];
        this.images[digest] = i;
      } else {
        this.images[digest].tags.push(tag);
      }

      this.tagMap[tag.toString()] = this.images[digest];
      if (tag === this.selectedTag) { this.selectedImage = this.images[digest]; }

    });
  }
}
