import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PathLocationStrategy, LocationStrategy } from '@angular/common';
import { CatalogService } from '../catalog.service';
import { Image } from '../models/image';
import { VersionSort } from '../version-sort';

@Component({
  selector: 'app-repository',
  templateUrl: './repository.component.html',
  styleUrls: ['./repository.component.css']
})
export class RepositoryComponent implements OnInit {

  public Name: String;

  public Tags: String[];

  public Images: { [id: string]: Image} = { };

  private objectKeys = Object.keys;

  constructor(private route: ActivatedRoute, private catalog: CatalogService) { }

  ngOnInit() {
    this.getRepo();
  }

  getRepo(): void {
    this.Name = this.route.snapshot.children[0].url.join('/');
    this.catalog.getTags(this.Name).subscribe(tags => {
      const latest = tags.filter(t => t === 'latest');
      const others = tags.filter(s => s !== 'latest');
      const final = latest.concat(others.sort(VersionSort.sort));
      this.Tags = final;
      this.Tags.forEach(t => this.getImage(t));
      /*if (latest) {
        this.catalog.getImage(this.Name, latest[0]).subscribe(i => {
          i.tags = [latest[0]];
          this.Images[i.setDigest.toString()] = i;
          others.forEach(t => this.getImage(t));
        });
      } else {
        this.Tags.forEach(t => this.getImage(t));
      }*/
    });
  }

  getImage(tag: String): void {
    this.catalog.getImage(this.Name, tag).subscribe(i => {
      if (!this.Images[i.setDigest.toString()]) {
        i.tags = [tag];
        this.Images[i.setDigest.toString()] = i;
      } else {
        this.Images[i.setDigest.toString()].tags.push(tag);
      }
    });
  }
}
