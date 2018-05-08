import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginFormComponent } from './login-form/login-form.component';
import { CatalogComponent } from './catalog/catalog.component';
import { SessionGuard } from './session-guard.service';
import { RepositoryComponent } from './repository/repository.component';


const routes: Routes = [
  { path: '', redirectTo: '/catalog', pathMatch: 'full' },
  { path: 'login', component: LoginFormComponent },
  { path: 'catalog', component: CatalogComponent, canActivate: [SessionGuard] },
  { path: 'repo', component: RepositoryComponent, canActivate: [SessionGuard], children: [{
    path: '**', component: RepositoryComponent
  }] }
];

@NgModule({
  imports: [ RouterModule.forRoot(routes) ],
  exports: [ RouterModule ]
})
export class AppRoutingModule {}
