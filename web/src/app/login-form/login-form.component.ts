import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { Router } from '@angular/router';
import { CatalogService } from '../catalog.service';
import { ConfigService } from '../config.service';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.css']
})
export class LoginFormComponent implements OnInit {

  constructor(
    public sessionService: SessionService,
    private catalogService: CatalogService,
    private configService: ConfigService,
    private router: Router) { }

  username: String;
  password: String;
  registry: String;
  registryLocked = false;
  remember: Boolean;

  ngOnInit() {
    if (this.configService.config.registry) {
      this.registry = this.configService.config.registry;
      this.registryLocked = true;
    }
  }

  logout() {
    this.sessionService.logout();
  }

  submit() {
    this.sessionService.login(this.username, this.password, this.registry, this.remember).subscribe(t => {
      console.log(`Got ${t.token}`);
      this.router.navigate(['']);
    });
  }
}
