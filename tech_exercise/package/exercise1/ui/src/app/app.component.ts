import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { AstronautDutyService } from './astronaut-duty.service';
import { PersonnelService } from './personnel.service';
import { ProcessLogService } from './process-log.service';
import { CreateDutyRequest, ProcessLog } from './personnel.models';
import {
  AstronautDutiesResponse,
  AstronautDuty,
  AstronautPerson
} from './astronaut-duty.models';

type SearchState = 'idle' | 'loading' | 'success' | 'empty' | 'not-found' | 'error';
type Workspace = 'duties' | 'people' | 'new-duty' | 'logs';
type RequestState = 'idle' | 'loading' | 'success' | 'empty' | 'not-found' | 'error';
type ActionState = 'idle' | 'loading' | 'success' | 'error';

function nonWhitespaceValidator(control: AbstractControl): ValidationErrors | null {
  return typeof control.value === 'string' && control.value.trim().length > 0
    ? null
    : { whitespace: true };
}

function isDutiesResponse(value: unknown): value is AstronautDutiesResponse {
  return typeof value === 'object' && value !== null && 'message' in value;
}

function apiMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && typeof error.error === 'object'
    && error.error !== null && 'message' in error.error
    && typeof error.error.message === 'string') {
    return error.error.message || fallback;
  }
  return fallback;
}

@Component({
  selector: 'acts-root',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly astronautDutyService = inject(AstronautDutyService);
  private readonly personnelService = inject(PersonnelService);
  private readonly processLogService = inject(ProcessLogService);
  readonly workspaces: { id: Workspace; label: string }[] = [
    { id: 'duties', label: 'Flight records' },
    { id: 'people', label: 'People directory' },
    { id: 'new-duty', label: 'Add duty' },
    { id: 'logs', label: 'Process logs' }
  ];
  activeWorkspace: Workspace = 'duties';

  readonly searchForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), nonWhitespaceValidator]
    })
  });

  readonly personLookupForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), nonWhitespaceValidator]
    })
  });
  readonly createPersonForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), nonWhitespaceValidator]
    })
  });
  readonly updatePersonForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), nonWhitespaceValidator]
    })
  });
  readonly createDutyForm = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), nonWhitespaceValidator]
    }),
    rank: new FormControl('', { nonNullable: true, validators: [Validators.required, nonWhitespaceValidator] }),
    dutyTitle: new FormControl('', { nonNullable: true, validators: [Validators.required, nonWhitespaceValidator] }),
    dutyStartDate: new FormControl('', { nonNullable: true, validators: [Validators.required] })
  });

  state: SearchState = 'idle';
  person: AstronautPerson | null = null;
  duties: AstronautDuty[] = [];
  errorMessage = '';
  people: AstronautPerson[] = [];
  peopleState: RequestState = 'idle';
  peopleError = '';
  personLookupState: RequestState = 'idle';
  selectedPerson: AstronautPerson | null = null;
  personLookupMessage = '';
  createPersonState: ActionState = 'idle';
  createPersonMessage = '';
  updatePersonState: ActionState = 'idle';
  updatePersonMessage = '';
  createDutyState: ActionState = 'idle';
  createDutyMessage = '';
  logs: ProcessLog[] = [];
  logsState: RequestState = 'idle';
  logsError = '';

  get nameControl(): FormControl<string> {
    return this.searchForm.controls.name;
  }

  setWorkspace(workspace: Workspace): void {
    this.activeWorkspace = workspace;
  }

  submitSearch(): void {
    if (this.state === 'loading') {
      return;
    }

    this.nameControl.markAsTouched();
    const name = this.nameControl.value.trim();
    if (this.searchForm.invalid) {
      return;
    }

    this.state = 'loading';
    this.person = null;
    this.duties = [];
    this.errorMessage = '';

    this.astronautDutyService.getDutiesByName(name).subscribe({
      next: (response) => this.handleResponse(response),
      error: (error: unknown) => this.handleError(error)
    });
  }

  private handleResponse(response: AstronautDutiesResponse): void {
    if (!response.success) {
      this.errorMessage = response.message || 'The request could not be completed.';
      this.state = response.responseCode === 404 ? 'not-found' : 'error';
      return;
    }

    this.person = response.person;
    this.duties = response.astronautDuties ?? [];
    this.state = this.duties.length > 0 ? 'success' : 'empty';
  }

  private handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      if (isDutiesResponse(error.error)) {
        this.person = error.error.person ?? null;
        this.duties = error.error.astronautDuties ?? [];
        this.errorMessage = error.error.message || 'The request could not be completed.';
        this.state = error.status === 404 && this.person && this.duties.length === 0
          ? 'empty'
          : error.status === 404
            ? 'not-found'
            : 'error';
        return;
      }

      this.errorMessage = error.status === 0
        ? 'Unable to reach the astronaut records service. Check your connection and try again.'
        : 'The astronaut records service could not complete your request. Please try again.';
    } else {
      this.errorMessage = 'Something went wrong while retrieving astronaut records.';
    }
    this.state = 'error';
  }

  loadPeople(): void {
    if (this.peopleState === 'loading') {
      return;
    }
    this.peopleState = 'loading';
    this.peopleError = '';
    this.personnelService.getPeople().subscribe({
      next: (response) => {
        if (!response.success) {
          this.people = [];
          this.peopleError = response.message || 'The people directory could not be loaded.';
          this.peopleState = response.responseCode === 404 ? 'not-found' : 'error';
          return;
        }
        this.people = response.people ?? [];
        this.peopleState = this.people.length > 0 ? 'success' : 'empty';
      },
      error: (error: unknown) => {
        this.peopleError = apiMessage(error, 'The people directory could not be loaded. Please try again.');
        this.peopleState = 'error';
      }
    });
  }

  submitPersonLookup(): void {
    if (this.personLookupState === 'loading') {
      return;
    }
    this.personLookupForm.controls.name.markAsTouched();
    if (this.personLookupForm.invalid) {
      return;
    }
    this.personLookupState = 'loading';
    this.selectedPerson = null;
    this.personLookupMessage = '';
    this.personnelService.getPersonByName(this.personLookupForm.controls.name.value.trim()).subscribe({
      next: (response) => {
        if (!response.success || !response.person) {
          this.personLookupMessage = response.message || 'That person could not be found.';
          this.personLookupState = response.responseCode === 404 ? 'not-found' : 'error';
          return;
        }
        this.selectedPerson = response.person;
        this.updatePersonForm.controls.name.setValue(response.person.name);
        this.personLookupState = 'success';
      },
      error: (error: unknown) => {
        this.personLookupMessage = apiMessage(error, 'The person lookup failed. Please try again.');
        this.personLookupState = error instanceof HttpErrorResponse && error.status === 404
          ? 'not-found'
          : 'error';
      }
    });
  }

  submitCreatePerson(): void {
    if (this.createPersonState === 'loading') {
      return;
    }
    this.createPersonForm.controls.name.markAsTouched();
    if (this.createPersonForm.invalid) {
      return;
    }
    this.createPersonState = 'loading';
    this.createPersonMessage = '';
    this.personnelService.createPerson(this.createPersonForm.controls.name.value.trim()).subscribe({
      next: (response) => {
        this.createPersonState = response.success ? 'success' : 'error';
        this.createPersonMessage = response.message || (response.success
          ? 'Person added to the directory.'
          : 'The person could not be added.');
        if (response.success) {
          this.createPersonForm.reset();
        }
      },
      error: (error: unknown) => {
        this.createPersonState = 'error';
        this.createPersonMessage = apiMessage(error, 'The person could not be added. Please try again.');
      }
    });
  }

  submitUpdatePerson(): void {
    if (this.updatePersonState === 'loading' || !this.selectedPerson) {
      return;
    }
    this.updatePersonForm.controls.name.markAsTouched();
    if (this.updatePersonForm.invalid) {
      return;
    }
    this.updatePersonState = 'loading';
    this.updatePersonMessage = '';
    this.personnelService.updatePersonName(
      this.selectedPerson.personId,
      this.updatePersonForm.controls.name.value.trim()
    ).subscribe({
      next: (response) => {
        this.updatePersonState = response.success ? 'success' : 'error';
        this.updatePersonMessage = response.message || (response.success
          ? 'Person name updated.'
          : 'The person name could not be updated.');
        if (response.success) {
          this.selectedPerson = { ...this.selectedPerson!, name: this.updatePersonForm.controls.name.value.trim() };
        }
      },
      error: (error: unknown) => {
        this.updatePersonState = 'error';
        this.updatePersonMessage = apiMessage(error, 'The person name could not be updated. Please try again.');
      }
    });
  }

  submitCreateDuty(): void {
    if (this.createDutyState === 'loading') {
      return;
    }
    this.createDutyForm.markAllAsTouched();
    if (this.createDutyForm.invalid) {
      return;
    }
    this.createDutyState = 'loading';
    this.createDutyMessage = '';
    const request: CreateDutyRequest = {
      name: this.createDutyForm.controls.name.value.trim(),
      rank: this.createDutyForm.controls.rank.value.trim(),
      dutyTitle: this.createDutyForm.controls.dutyTitle.value.trim(),
      dutyStartDate: this.createDutyForm.controls.dutyStartDate.value
    };
    this.personnelService.createAstronautDuty(request).subscribe({
      next: (response) => {
        this.createDutyState = response.success ? 'success' : 'error';
        this.createDutyMessage = response.message || (response.success
          ? 'Astronaut duty added.'
          : 'The astronaut duty could not be added.');
        if (response.success) {
          this.createDutyForm.reset();
        }
      },
      error: (error: unknown) => {
        this.createDutyState = 'error';
        this.createDutyMessage = apiMessage(error, 'The astronaut duty could not be added. Please try again.');
      }
    });
  }

  loadLogs(): void {
    if (this.logsState === 'loading') {
      return;
    }
    this.logsState = 'loading';
    this.logsError = '';
    this.processLogService.getAllLogs().subscribe({
      next: (logs) => {
        this.logs = logs ?? [];
        this.logsState = this.logs.length > 0 ? 'success' : 'empty';
      },
      error: (error: unknown) => {
        this.logsError = apiMessage(error, 'Process logs could not be loaded. Please try again.');
        this.logsState = 'error';
      }
    });
  }
}
