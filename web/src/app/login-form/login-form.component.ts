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

import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { SessionService } from '../session.service';
import { Router, ActivatedRoute } from '@angular/router';
import { ConfigService } from '../config.service';
import { isError } from '../web-service';
import { Title } from '@angular/platform-browser';
import { Theme } from '../models/theme';
import { SortOrder } from '../models/sort-order';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.css']
})
export class LoginFormComponent implements OnInit {

  constructor(
    public sessionService: SessionService,
    private configService: ConfigService,
    private titleService: Title,
    private route: ActivatedRoute,
    private router: Router) { }

  username: string;
  password: string;
  registry: string;
  
  passwordVisible = false;
  remember = true;
  anonymous = false;
  themes: Theme[];
  showPreview = false;

  get banner() {
    return this.configService.config.loginBanner;
  }

  public sortOptions = Object.keys(SortOrder);
  public get defaultSort() {
    return this.configService.getDefaultSort();
  }
  public set defaultSort(value: string) {
    this.configService.setDefaultSort(value as SortOrder);
  }

  get selectedTheme(): string {
    return this.configService.currentTheme.name;
  }
  set selectedTheme(theme: string) {
    this.configService.setTheme(this.themes.find(t => t.name === theme));
  }

  get usernamePlaceholder(): string {
    return this.configService.config.userName.placeholderText ?? "Username";
  }

  get usernameHidden(): boolean {
    return this.configService.config.userName.isHidden || this.anonymous;
  }

  get usernameLocked(): boolean {
    return this.configService.config.userName.isReadonly ?? false;
  }

  get passwordPlaceholder(): string {
    return this.configService.config.password.placeholderText ?? "Password";
  }

  get passwordHidden(): boolean {
    return this.configService.config.password.isHidden || this.anonymous;
  }

  get passwordLocked(): boolean {
    return this.configService.config.password.isReadonly ?? false;
  }

  get passwordType(): string {
    return this.passwordVisible ? "textbox" : "password";
  }

  get registryPlaceholder(): string {
    return this.configService.config.registry.placeholderText ?? "Registry";
  }

  get registryHidden(): boolean {
    return this.configService.config.registry.isHidden || this.anonymous;
  }

  get registryLocked(): boolean {
    return this.configService.config.registry.isReadonly ?? false;
  }

  
  errorMessage: string;
  isErrored = false;

  forwardingRoute: string;

  ngOnInit() {
    if (this.configService.config.registry.isDefault) {
      this.registry = this.configService.config.registry.placeholderText;
    }
    
    if (this.configService.config.userName.isDefault) {
      this.username = this.configService.config.userName.placeholderText;
    }
    
    const title = (this.registry || 'Whalerator') + ' Login';
    this.titleService.setTitle(title);

    this.route.queryParams.subscribe(p => {
      this.forwardingRoute = p['requested'];
    });

    if (this.configService.config.autoLogin) {
      this.anonymous = true;
      if (!this.sessionService.sessionToken) {
        this.submit();
      }
    }

    // only offer the selector if there is more than one theme to choose from
    if (this.configService.config.themes && this.configService.config.themes.length > 1) {
      this.themes = this.configService.config.themes;
    }
  }

  // resets all repo-level sort preferences
  resetSorts() {
    this.configService.resetSorts();
    this.configService.setDefaultSort(SortOrder.Semver);
  }

  logout() {
    this.sessionService.logout();
  }

  submit() {
    this.isErrored = false;
    const username = this.anonymous ? null : this.username;
    const password = this.anonymous ? null : this.password;
    this.sessionService.login(username, password, this.registry, this.remember).subscribe(t => {
      if (isError(t)) {
        this.errorMessage = 'Login failed.';
        this.isErrored = true;
      } else {
        this.router.navigateByUrl((this.forwardingRoute || '/catalog'));
      }
    });
  }
}
