import { TestBed } from '@angular/core/testing';

import { MpmDataService } from './mpm-data.service';

describe('MpmDataService', () => {
  let service: MpmDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MpmDataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
