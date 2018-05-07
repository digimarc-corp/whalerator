import { TestBed, inject } from '@angular/core/testing';

import { SessionGuard } from './session-guard.service';

describe('SessionGuardService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SessionGuard]
    });
  });

  it('should be created', inject([SessionGuard], (service: SessionGuard) => {
    expect(service).toBeTruthy();
  }));
});
