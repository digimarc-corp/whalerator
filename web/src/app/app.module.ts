import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { AppComponent } from './app.component';
import { LoginFormComponent } from './login-form/login-form.component';
import { AppRoutingModule } from './/app-routing.module';
import { CatalogComponent } from './catalog/catalog.component';
import { RepositoryComponent } from './repository/repository.component';
import { TagSortPipe } from './tag-sort.pipe';

import { MarkdownModule } from 'ngx-markdown';
import { AboutComponent } from './about/about.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginFormComponent,
    CatalogComponent,
    RepositoryComponent,
    TagSortPipe,
    AboutComponent
  ],
  imports: [
    MarkdownModule.forRoot(),
    BrowserModule,
    FormsModule,
    HttpClientModule,
    AppRoutingModule
  ],
  providers: [ ],
  bootstrap: [AppComponent]
})
export class AppModule { }
