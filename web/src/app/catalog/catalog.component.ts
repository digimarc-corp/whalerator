import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { Repository } from '../repository';
import { CatalogService } from '../catalog.service';

@Component({
  selector: 'app-catalog',
  templateUrl: './catalog.component.html',
  styleUrls: ['./catalog.component.css']
})
export class CatalogComponent implements OnInit {

  public repos: String[];

  constructor(private sessionService: SessionService, private catalogService: CatalogService) { }

  ngOnInit() {
    this.catalogService.getRepos().subscribe(r => this.repos = r);
  }
}
