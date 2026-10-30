import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../environments/environment';
import { PersonnelService } from './personnel.service';

describe('PersonnelService', () => {
  let service: PersonnelService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PersonnelService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PersonnelService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('uses the person routes, URL-encodes names, and sends JSON string bodies', () => {
    service.getPeople().subscribe();
    const peopleRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}/Person/GetPeople`);
    expect(peopleRequest.request.method).toBe('GET');
    peopleRequest.flush({ success: true, people: [] });

    service.getPersonByName('Sally Ride & Crew').subscribe();
    const personRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}/Person/GetPersonByName/Sally%20Ride%20%26%20Crew`
    );
    expect(personRequest.request.method).toBe('GET');
    personRequest.flush({ success: true, person: null });

    service.createPerson('Sally Ride').subscribe();
    const createRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}/Person/CreatePerson`);
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toBe('"Sally Ride"');
    expect(createRequest.request.headers.get('Content-Type')).toBe('application/json');
    createRequest.flush({ success: true, id: 1 });

    service.updatePersonName(1, 'Dr. Sally Ride').subscribe();
    const updateRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}/Person/UpdatePersonById/1`
    );
    expect(updateRequest.request.method).toBe('PATCH');
    expect(updateRequest.request.body).toBe('"Dr. Sally Ride"');
    expect(updateRequest.request.headers.get('Content-Type')).toBe('application/json');
    updateRequest.flush({ success: true, id: 1 });
  });

  it('posts the expected astronaut duty payload', () => {
    const requestBody = {
      name: 'Sally Ride',
      rank: 'Captain',
      dutyTitle: 'Mission Specialist',
      dutyStartDate: '1983-06-18'
    };
    service.createAstronautDuty(requestBody).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}/AstronautDuty/CreateAstronautDuty`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBe(JSON.stringify(requestBody));
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ success: true, id: 1 });
  });
});
