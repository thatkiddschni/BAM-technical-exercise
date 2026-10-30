import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  CreateDutyRequest,
  MutationResponse,
  PeopleResponse,
  PersonResponse
} from './personnel.models';

@Injectable({ providedIn: 'root' })
export class PersonnelService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  getPeople(): Observable<PeopleResponse> {
    return this.http.get<PeopleResponse>(`${this.apiBaseUrl}/Person/GetPeople`);
  }

  getPersonByName(name: string): Observable<PersonResponse> {
    return this.http.get<PersonResponse>(
      `${this.apiBaseUrl}/Person/GetPersonByName/${encodeURIComponent(name)}`
    );
  }

  createPerson(name: string): Observable<MutationResponse> {
    return this.http.post<MutationResponse>(
      `${this.apiBaseUrl}/Person/CreatePerson`,
      JSON.stringify(name),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  updatePersonName(personId: number, newName: string): Observable<MutationResponse> {
    return this.http.patch<MutationResponse>(
      `${this.apiBaseUrl}/Person/UpdatePersonById/${personId}`,
      JSON.stringify(newName),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  createAstronautDuty(request: CreateDutyRequest): Observable<MutationResponse> {
    return this.http.post<MutationResponse>(
      `${this.apiBaseUrl}/AstronautDuty/CreateAstronautDuty`,
      JSON.stringify(request),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }
}
