import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { CatalogService } from '../catalog.service';
import { Repository } from '../models/repository';
import { isError } from '../web-service';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.scss']
})
export class CatalogComponent implements OnInit {

  public repos: Repository[];

  public errorMessage: String;

  constructor(private sessionService: SessionService,
    private catalogService: CatalogService,
    private titleService: Title) { }

  ngOnInit() {
    this.titleService.setTitle(this.sessionService.activeRegistry + ' - Catalog');
    this.catalogService.getRepos().subscribe(r => {
      if (isError(r)) {
        this.errorMessage = 'There was an error fetching the repository catalog.';
        console.log(r.message);
      } else {
        this.repos = r;
      }
    });
  }
}
