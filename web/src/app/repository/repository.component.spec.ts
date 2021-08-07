import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { RepositoryComponent } from './repository.component';
import { FormsModule } from '@angular/forms';
import { DocumentComponent } from '../document/document.component';
import { MarkdownComponent } from 'ngx-markdown';
import { ScanResultComponent } from '../scan-result/scan-result.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { SortOrder } from '../models/sort-order';

describe('RepositoryComponent', () => {
  let component: RepositoryComponent;
  let fixture: ComponentFixture<RepositoryComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [ RouterTestingModule, HttpClientTestingModule, FormsModule ],
      declarations: [ RepositoryComponent, DocumentComponent, MarkdownComponent, ScanResultComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RepositoryComponent);
    component = fixture.componentInstance;
    component.sort = SortOrder.Semver;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
