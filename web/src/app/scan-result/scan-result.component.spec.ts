import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ScanResultComponent } from './scan-result.component';
import { FormsModule } from '@angular/forms';
import { ScanComponent } from '../models/scanComponent';
import { ScanResult } from '../models/scanResult';

describe('ScanResultComponent', () => {
  let component: ScanResultComponent;
  let fixture: ComponentFixture<ScanResultComponent>;

  beforeEach(async(() => {
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
