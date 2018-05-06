import { Component, OnInit } from '@angular/core';
import { SessionService } from '../session.service';

@Component({
  selector: 'app-login-form',
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.css']
})
export class LoginFormComponent implements OnInit {

  constructor(public sessionService: SessionService) { }

  username: String;
  password: String;
  registry: String;

  ngOnInit() {
  }

  submit() {
    this.sessionService.getToken(this.username, this.password, this.registry);
  }
}
