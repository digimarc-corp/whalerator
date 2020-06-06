import { TestBed, inject } from '@angular/core/testing';

import { SessionService } from './session.service';

import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('SessionService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      providers: [SessionService]
    });
  });

  it('should be created', inject([SessionService], (service: SessionService) => {
    expect(service).toBeTruthy();
  }));
});
