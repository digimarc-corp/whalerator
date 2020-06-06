import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RepositoryComponent } from './repository.component';
import { FormsModule } from '@angular/forms';
import { DocumentComponent } from '../document/document.component';
import { MarkdownComponent } from 'ngx-markdown';
import { ScanResultComponent } from '../scan-result/scan-result.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('RepositoryComponent', () => {
  let component: RepositoryComponent;
  let fixture: ComponentFixture<RepositoryComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      imports: [ RouterTestingModule, HttpClientTestingModule, FormsModule ],
      declarations: [ RepositoryComponent, DocumentComponent, MarkdownComponent, ScanResultComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RepositoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
