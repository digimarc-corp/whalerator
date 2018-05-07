import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.css']
})
export class LoginFormComponent implements OnInit {

  constructor(
    public sessionService: SessionService,
    private router: Router) { }

  username: String;
  password: String;
  registry: String;
  remember: Boolean;

  ngOnInit() {
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
