import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ScanResultComponent } from './scan-result.component';
import { FormsModule } from '@angular/forms';
import { ScanComponent } from '../models/scan-component';
import { ScanResult } from '../models/scan-result';

describe('ScanResultComponent', () => {
  let component: ScanResultComponent;
  let fixture: ComponentFixture<ScanResultComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      imports: [FormsModule],
      declarations: [ScanResultComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ScanResultComponent);
    component = fixture.componentInstance;
    component.scan = new ScanResult;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
