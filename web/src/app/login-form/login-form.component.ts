import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { Router, ActivatedRoute } from '@angular/router';
import { CatalogService } from '../catalog.service';
import { ConfigService } from '../config.service';
import { isError } from '../web-service';
import { Title } from '@angular/platform-browser';

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
    private titleService: Title,
    private route: ActivatedRoute,
    private router: Router) { }

  username: String;
  password: String;
  registry: String;
  registryLocked = false;
  remember: Boolean;

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
  }

  logout() {
    this.sessionService.logout();
  }

  submit() {
    this.isErrored = false;
    this.sessionService.login(this.username, this.password, this.registry, this.remember).subscribe(t => {
      if (isError(t)) {
        this.errorMessage = 'Login failed.';
        this.isErrored = true;
      } else {
        console.log(`Got ${t.token}`);
        this.router.navigate([this.forwardingRoute || '']);
      }
    });
  }
}
