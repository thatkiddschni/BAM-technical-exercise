import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { ProcessLog } from './personnel.models';

@Injectable({ providedIn: 'root' })
export class ProcessLogService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');

  getAllLogs(): Observable<ProcessLog[]> {
    return this.http.get<ProcessLog[]>(`${this.apiBaseUrl}/ProcessLogger/GetAllLogs`);
  }
}
