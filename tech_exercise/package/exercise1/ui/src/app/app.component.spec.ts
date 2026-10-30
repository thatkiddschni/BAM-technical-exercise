import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AstronautPerson } from './astronaut-duty.models';
import { AppComponent } from './app.component';
import { AstronautDutyService } from './astronaut-duty.service';

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;
  let httpTestingController: HttpTestingController;

  const person: AstronautPerson = {
    personId: 7,
    name: 'Sally Ride',
    currentRank: 'Captain',
    currentDutyTitle: 'Astronaut',
    careerStartDate: '1978-01-01T00:00:00',
    careerEndDate: null
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        AstronautDutyService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => httpTestingController.verify());

  it('shows validation feedback and does not request a blank name', () => {
    component.submitSearch();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Enter a name to search the records.');
    httpTestingController.expectNone((request) => request.method === 'GET');
  });

  it('shows progress, then renders the person and duty results', () => {
    component.searchForm.controls.name.setValue('Sally Ride');
    component.submitSearch();
    fixture.detectChanges();

    expect(component.state).toBe('loading');
    expect(fixture.nativeElement.textContent).toContain('Retrieving flight records');
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBeTrue();

    const request = httpTestingController.expectOne(
      '/api/AstronautDuty/GetAstronautDutiesByName/Sally%20Ride'
    );
    request.flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      person,
      astronautDuties: [
        {
          id: 2,
          personId: 7,
          rank: 'Captain',
          dutyTitle: 'Mission Specialist',
          dutyStartDate: '1983-06-18T00:00:00',
          dutyEndDate: null
        }
      ]
    });
    fixture.detectChanges();

    expect(component.state).toBe('success');
    expect(fixture.nativeElement.textContent).toContain('Sally Ride');
    expect(fixture.nativeElement.textContent).toContain('Mission Specialist');
    expect(fixture.nativeElement.textContent).toContain('CURRENT');
  });

  it('shows a clear message when the person is not found', () => {
    component.searchForm.controls.name.setValue('Unknown Person');
    component.submitSearch();

    httpTestingController.expectOne(
      '/api/AstronautDuty/GetAstronautDutiesByName/Unknown%20Person'
    ).flush({
      success: false,
      message: "Person 'Unknown Person' was not found.",
      responseCode: 404,
      person: null,
      astronautDuties: []
    }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(component.state).toBe('not-found');
    expect(fixture.nativeElement.textContent).toContain("Person 'Unknown Person' was not found.");
  });

  it('distinguishes a person with no astronaut duties from a missing person', () => {
    component.searchForm.controls.name.setValue('New Astronaut');
    component.submitSearch();

    httpTestingController.expectOne(
      '/api/AstronautDuty/GetAstronautDutiesByName/New%20Astronaut'
    ).flush({
      success: false,
      message: "No astronaut duties were found for person 'New Astronaut'.",
      responseCode: 404,
      person: { ...person, name: 'New Astronaut' },
      astronautDuties: []
    }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(component.state).toBe('empty');
    expect(fixture.nativeElement.textContent).toContain('New Astronaut has no recorded duties.');
  });

  it('shows a helpful error when the API cannot be reached', () => {
    component.searchForm.controls.name.setValue('Sally Ride');
    component.submitSearch();

    httpTestingController.expectOne(
      '/api/AstronautDuty/GetAstronautDutiesByName/Sally%20Ride'
    ).error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(component.state).toBe('error');
    expect(fixture.nativeElement.textContent).toContain('Unable to reach the astronaut records service.');
  });

  it('loads the people list and shows an empty directory state', () => {
    component.setWorkspace('people');
    component.loadPeople();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Loading the personnel directory');
    httpTestingController.expectOne('/api/Person/GetPeople').flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      people: [person]
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Sally Ride');
    expect(fixture.nativeElement.textContent).toContain('All people in the personnel directory');

    component.loadPeople();
    httpTestingController.expectOne('/api/Person/GetPeople').flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      people: []
    });
    fixture.detectChanges();
    expect(component.peopleState).toBe('empty');
    expect(fixture.nativeElement.textContent).toContain('No people are currently listed.');
  });

  it('looks up a person and updates their name with duplicate submission protection', () => {
    component.setWorkspace('people');
    component.personLookupForm.controls.name.setValue('Sally Ride');
    component.submitPersonLookup();
    component.submitPersonLookup();

    const lookup = httpTestingController.expectOne('/api/Person/GetPersonByName/Sally%20Ride');
    expect(lookup.request.method).toBe('GET');
    lookup.flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      person
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Update name');

    component.updatePersonForm.controls.name.setValue('Dr. Sally Ride');
    component.submitUpdatePerson();
    component.submitUpdatePerson();
    const update = httpTestingController.expectOne('/api/Person/UpdatePersonById/7');
    expect(update.request.method).toBe('PATCH');
    expect(update.request.body).toBe('"Dr. Sally Ride"');
    expect(update.request.headers.get('Content-Type')).toBe('application/json');
    update.flush({ success: true, message: 'Successful', responseCode: 200, id: 7 });

    expect(component.selectedPerson?.name).toBe('Dr. Sally Ride');
    expect(component.updatePersonState).toBe('success');
  });

  it('creates a person and handles an API conflict response', () => {
    component.setWorkspace('people');
    component.createPersonForm.controls.name.setValue('Sally Ride');
    component.submitCreatePerson();
    const request = httpTestingController.expectOne('/api/Person/CreatePerson');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBe('"Sally Ride"');
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({
      success: false,
      message: "A person named 'Sally Ride' already exists.",
      responseCode: 409,
      id: 0
    }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(component.createPersonState).toBe('error');
    expect(fixture.nativeElement.textContent).toContain('already exists');
  });

  it('creates a person from the form and clears it after success', () => {
    component.setWorkspace('people');
    component.createPersonForm.controls.name.setValue('Mae Jemison');
    component.submitCreatePerson();

    const request = httpTestingController.expectOne('/api/Person/CreatePerson');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBe('"Mae Jemison"');
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ success: true, message: 'Successful', responseCode: 200, id: 8 });

    expect(component.createPersonState).toBe('success');
    expect(component.createPersonForm.controls.name.value).toBe('');
    expect(component.createPersonMessage).toBe('Successful');
  });

  it('validates and creates an astronaut duty', () => {
    component.setWorkspace('new-duty');
    component.submitCreateDuty();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Person name');
    httpTestingController.expectNone('/api/AstronautDuty/CreateAstronautDuty');

    component.createDutyForm.setValue({
      name: 'Sally Ride',
      rank: 'Captain',
      dutyTitle: 'Mission Specialist',
      dutyStartDate: '1983-06-18'
    });
    component.submitCreateDuty();
    component.submitCreateDuty();
    const request = httpTestingController.expectOne('/api/AstronautDuty/CreateAstronautDuty');
    expect(request.request.body).toBe(JSON.stringify({
      name: 'Sally Ride',
      rank: 'Captain',
      dutyTitle: 'Mission Specialist',
      dutyStartDate: '1983-06-18'
    }));
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    request.flush({ success: true, message: 'Successful', responseCode: 200, id: 3 });
    expect(component.createDutyState).toBe('success');
    expect(component.createDutyForm.controls.name.value).toBe('');
  });

  it('loads process logs without rendering exception details', () => {
    component.setWorkspace('logs');
    component.loadLogs();
    const request = httpTestingController.expectOne('/api/ProcessLogger/GetAllLogs');
    expect(request.request.method).toBe('GET');
    request.flush([{
      id: 1,
      operation: 'GetPeople',
      component: 'PersonController',
      outcome: 'Exception',
      message: 'The request failed.',
      exceptionType: 'System.Exception',
      exceptionDetails: 'private stack trace',
      occurredAtUtc: '2026-10-29T12:00:00Z'
    }]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Exception details are hidden.');
    expect(fixture.nativeElement.textContent).not.toContain('The request failed.');
    expect(fixture.nativeElement.textContent).not.toContain('private stack trace');
    expect(fixture.nativeElement.textContent).not.toContain('System.Exception');
  });

  it('shows a useful error when process logs cannot be reached', () => {
    component.setWorkspace('logs');
    component.loadLogs();
    httpTestingController.expectOne('/api/ProcessLogger/GetAllLogs')
      .error(new ProgressEvent('error'));
    fixture.detectChanges();

    expect(component.logsState).toBe('error');
    expect(fixture.nativeElement.textContent).toContain('Process logs could not be loaded.');
  });
});
