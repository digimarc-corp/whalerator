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

  username: String;
  password: String;
  registry: String;
  registryLocked = false;
  remember: Boolean = true;
  anonymous: Boolean = false;
  themes: Theme[];
  showPreview = false;

  lorem = '## Readme\n' +
    'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\n' +
    '```\nprint("Hello, world!")```';

  get selectedTheme(): String {
    return this.configService.currentTheme.name;
  }
  set selectedTheme(theme: String) {
    this.configService.setTheme(this.themes.find(t => t.name === theme));
  }

  errorMessage: String;
  isErrored = false;

  forwardingRoute: String;

  ngOnInit() {
    if (this.configService.config.registry) {
      this.registry = this.configService.config.registry;
      this.registryLocked = true;
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

    this.themes = this.configService.config.themes;
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
        this.router.navigateByUrl((this.forwardingRoute || '/catalog').toString());
      }
    });
  }
}
