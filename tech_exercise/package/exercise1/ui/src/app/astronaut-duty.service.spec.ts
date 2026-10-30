import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { AstronautDutyService } from './astronaut-duty.service';

describe('AstronautDutyService', () => {
  let service: AstronautDutyService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AstronautDutyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(AstronautDutyService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('requests duties by an encoded name using the configured API base URL', () => {
    service.getDutiesByName('Sally Ride & Crew').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}/AstronautDuty/GetAstronautDutiesByName/Sally%20Ride%20%26%20Crew`
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      person: null,
      astronautDuties: []
    });
  });
});
