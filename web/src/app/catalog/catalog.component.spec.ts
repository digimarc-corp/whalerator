import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CatalogComponent } from './catalog.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ConfigService } from '../config.service';
import { FormsModule } from '@angular/forms';
import { MarkdownComponent, MarkdownService } from 'ngx-markdown';

describe('CatalogComponent', () => {
  let component: CatalogComponent;
  let fixture: ComponentFixture<CatalogComponent>;

  beforeEach(async(() => {
    const configSpy = { pagerSize: 25, config: { catalogBanner: 'Hi there' } };

    TestBed.configureTestingModule({
      providers: [
        { provide: ConfigService, useValue: configSpy },
        // why is this not necessary on other components? Clearly missing something obvious here...
        { provide: MarkdownService, useValue: { compile: () => null, highlight: () => { } } }
      ],
      imports: [ RouterTestingModule, HttpClientTestingModule, FormsModule ],
      declarations: [ CatalogComponent, MarkdownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CatalogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
