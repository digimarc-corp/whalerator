import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { LoginFormComponent } from './login-form.component';

import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { FormsModule } from '@angular/forms';
import { MarkdownComponent } from 'ngx-markdown';
import { ConfigService } from '../config.service';

describe('LoginFormComponent', () => {
  let component: LoginFormComponent;
  let fixture: ComponentFixture<LoginFormComponent>;

  beforeEach(waitForAsync(() => {
    const configSpy = { config: { get: jasmine.createSpyObj('Config', [ 'registry' ]) } };

    TestBed.configureTestingModule({
      providers: [
        { provide: ConfigService, useValue: configSpy },
      ],
      imports: [HttpClientTestingModule, RouterTestingModule, FormsModule],
      declarations: [ LoginFormComponent, MarkdownComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(LoginFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
