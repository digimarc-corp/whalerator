import { TestBed, inject } from '@angular/core/testing';

import { CatalogService } from './catalog.service';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('CatalogService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ RouterTestingModule, HttpClientTestingModule ],
      providers: [CatalogService]
    });
  });

  it('should be created', inject([CatalogService], (service: CatalogService) => {
    expect(service).toBeTruthy();
  }));
});
