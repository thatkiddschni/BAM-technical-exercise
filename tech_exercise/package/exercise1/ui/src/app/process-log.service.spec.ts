import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { ProcessLogService } from './process-log.service';

describe('ProcessLogService', () => {
  let service: ProcessLogService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProcessLogService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ProcessLogService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('retrieves the read-only process log list', () => {
    service.getAllLogs().subscribe();
    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}/ProcessLogger/GetAllLogs`
    );
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
