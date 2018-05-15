import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { CatalogService } from '../catalog.service';
import { Repository } from '../models/repository';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.scss']
})
export class CatalogComponent implements OnInit {

  public repos: Repository[];

  constructor(private sessionService: SessionService, private catalogService: CatalogService) { }

  ngOnInit() {
    this.catalogService.getRepos().subscribe(r => this.repos = r);//.sort(this.repositorySort));
  }

  repositorySort(a: Repository, b: Repository): number {
    return a.name === b.name ? 0 : a.name > b.name ? 1 : -1;
  }
}
