/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

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
import { DocumentComponent } from './document/document.component';
import { ScanResultComponent } from './scan-result/scan-result.component';
import { CookieService } from 'ngx-cookie-service';

@NgModule({
  declarations: [
    AppComponent,
    LoginFormComponent,
    CatalogComponent,
    RepositoryComponent,
    TagSortPipe,
    AboutComponent,
    DocumentComponent,
    ScanResultComponent,
  ],
  imports: [
    MarkdownModule.forRoot(),
    BrowserModule,
    FormsModule,
    HttpClientModule,
    AppRoutingModule
  ],
  providers: [ CookieService ],
  bootstrap: [AppComponent]
})
export class AppModule { }
