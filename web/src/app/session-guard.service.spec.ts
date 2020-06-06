import { TestBed, inject } from '@angular/core/testing';

import { SessionGuard } from './session-guard.service';

import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('SessionGuardService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      providers: [SessionGuard]
    });
  });

  it('should be created', inject([SessionGuard], (service: SessionGuard) => {
    expect(service).toBeTruthy();
  }));
});
