import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { Observable } from 'rxjs';
import { DailyAttendanceSummaryDto } from '../models/daily-attendance-summary-dto';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {

  private readonly http = inject(HttpClient);

  private apiUrl = environment.apiUrl + '/api/Attendance';

  checkIn() : Observable<any>{
    return this.http.post(`${this.apiUrl}/check-in`,{});
  }
  checkOut(): Observable<any>{
    return this.http.post(`${this.apiUrl}/check-out`,{});
  }
  getDailySummary() : Observable<DailyAttendanceSummaryDto>{
    return this.http.get<DailyAttendanceSummaryDto>(`${this.apiUrl}/summary`);
  }
}
