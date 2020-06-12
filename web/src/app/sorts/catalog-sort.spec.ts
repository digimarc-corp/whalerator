import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { SemverSort } from './semver-sort';
import { Repository } from '../models/repository';
import { CatalogSort } from './catalog-sort';

describe('CatalogSort', () => {

    beforeEach(async(() => {
        TestBed.configureTestingModule({})
            .compileComponents();
    }));

    it('should sort list', () => {
        const unsorted = [
            'test',
            'alpha',
            'test',
            new Repository({ name: 'baker' }),
            'foo/bar',
            '123',
            new Repository({ name: 'able' }),
            'joe',
            'xyz',
            new Repository({ name: 'charlie' }),
            'foo',
        ];
        const sorted = [
            '123',
            new Repository({ name: 'able' }),
            'alpha',
            new Repository({ name: 'baker' }),
            new Repository({ name: 'charlie' }),
            'foo',
            'foo/bar',
            'joe',
            'test',
            'test',
            'xyz',
        ];

        unsorted.sort(CatalogSort.sort);

        expect(unsorted).toEqual(sorted);
    });

});
