export interface AstronautDuty {
  id: number;
  personId: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface AstronautPerson {
  personId: number;
  name: string;
  currentRank: string | null;
  currentDutyTitle: string | null;
  careerStartDate: string | null;
  careerEndDate: string | null;
}

export interface AstronautDutiesResponse {
  success: boolean;
  message: string;
  responseCode: number;
  person: AstronautPerson | null;
  astronautDuties: AstronautDuty[];
}
