import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { SessionService } from './session.service';
import { Location } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class SessionGuard implements CanActivate {
  constructor(private authService: SessionService, private router: Router, private location: Location) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.authService.sessionToken) {
      this.router.navigate(['/login'], { queryParams: { requested: this.location.path() } });
      return false;
    } else {
      return true;
    }
  }
}
