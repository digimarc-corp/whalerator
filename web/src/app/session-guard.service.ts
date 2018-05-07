import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class SessionGuard implements CanActivate {
  constructor(private authService: SessionService, private router: Router) {}

  canActivate() {
    if (!this.authService.sessionToken) {
      this.router.navigate(['/login']);
      return false;
    } else {
      return true;
    }
  }
}
