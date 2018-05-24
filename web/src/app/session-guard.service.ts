import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot } from '@angular/router';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class SessionGuard implements CanActivate {
  constructor(private authService: SessionService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const parts = this.routeParts(route);
    if (!this.authService.sessionToken) {
      this.router.navigate(['/login'], { queryParams: { requested: parts.join('/') } });
      return false;
    } else {
      return true;
    }
  }

  routeParts(route: ActivatedRouteSnapshot): String[] {
    const parts: String[] = [];
    route.url.forEach(u => parts.push(u.toString()));
    route.children.forEach(c => this.routeParts(c).forEach(p => parts.push(p)));
    return parts;
  }
}