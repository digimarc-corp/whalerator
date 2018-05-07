import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginFormComponent } from './login-form/login-form.component';
import { CatalogComponent } from './catalog/catalog.component';
import { SessionGuard } from './session-guard.service';


const routes: Routes = [
  { path: '', redirectTo: '/catalog', pathMatch: 'full' },
  // { path: 'dashboard', component: DashboardComponent },
  // { path: 'detail/:id', component: HeroDetailComponent },
  { path: 'login', component: LoginFormComponent },
  { path: 'catalog', component: CatalogComponent, canActivate: [SessionGuard] }
];

@NgModule({
  imports: [ RouterModule.forRoot(routes) ],
  exports: [ RouterModule ]
})
export class AppRoutingModule {}
