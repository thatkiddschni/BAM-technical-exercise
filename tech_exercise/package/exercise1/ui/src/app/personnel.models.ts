import { AstronautPerson } from './astronaut-duty.models';

export interface PeopleResponse {
  success: boolean;
  message: string;
  responseCode: number;
  people: AstronautPerson[];
}

export interface PersonResponse {
  success: boolean;
  message: string;
  responseCode: number;
  person: AstronautPerson | null;
}

export interface MutationResponse {
  success: boolean;
  message: string;
  responseCode: number;
  id?: number | null;
}

export interface CreateDutyRequest {
  name: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
}

export interface ProcessLog {
  id: number;
  operation: string;
  component: string;
  outcome: string;
  message: string;
  occurredAtUtc: string;
}
