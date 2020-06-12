import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { SemverSort } from './semver-sort';

describe('SemverSort', () => {

  beforeEach(async(() => {
    TestBed.configureTestingModule({})
      .compileComponents();
  }));

  it('should sort list', () => {
    const unsorted = [
      'qa1.2.3',
      'alpha.23',
      '0',
      '1.2.3-a4',
      '1.2',
      'latest',
      'latest-rc',
      'v1.2.2',
      '1.2.3',
      'dev-9',
      'latest-beta',
      '1.2.3-rc3',
      '1',
      '0.2.3.4',
      '1.0.0',
    ];
    const sorted = [
      'latest',
      'latest-beta',
      'latest-rc',
      '1',
      '1.2',
      '1.2.3',
      'v1.2.2',
      '1.0.0',
      '0',
      '1.2.3-a4',
      '1.2.3-rc3',
      '0.2.3.4',
      'alpha.23',
      'dev-9',
      'qa1.2.3',
    ];

    unsorted.sort(SemverSort.sort);

    expect(unsorted).toEqual(sorted);
  });

});
