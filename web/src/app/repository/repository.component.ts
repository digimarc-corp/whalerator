import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PathLocationStrategy, LocationStrategy } from '@angular/common';

@Component({
  selector: 'app-repository',
  templateUrl: './repository.component.html',
  styleUrls: ['./repository.component.css']
})
export class RepositoryComponent implements OnInit {

  public Name: String;

  constructor(private route: ActivatedRoute) { }

  ngOnInit() {
    this.getRepo();
  }

  getRepo(): void {
    this.Name = this.route.snapshot.children[0].url.join('/');
  }
}
