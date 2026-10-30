import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { AstronautDutiesResponse } from './astronaut-duty.models';

@Injectable({ providedIn: 'root' })
export class AstronautDutyService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  getDutiesByName(name: string): Observable<AstronautDutiesResponse> {
    const encodedName = encodeURIComponent(name);
    return this.http.get<AstronautDutiesResponse>(
      `${this.apiBaseUrl}/AstronautDuty/GetAstronautDutiesByName/${encodedName}`
    );
  }
}
