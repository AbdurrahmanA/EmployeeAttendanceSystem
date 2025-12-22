import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { audit, Observable } from 'rxjs';
import { AuditLogDto, PaginatedResult } from '../models/audit-log.dto';
import { UpdateEmployeeDto, EmployeeDto } from '../models/update-employee-dto';
import * as CryptoJS from 'crypto-js';
@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl + '/api';


  private hashPassword(password: string): string{
    if(!password) return "";
    return CryptoJS.SHA256(password).toString(CryptoJS.enc.Hex);  
  }

  getAttendanceLogs(startDate?: string, endDate?: string): Observable<any> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get(`${this.apiUrl}/report/admin/all-logs`, { params });
  }

  downloadAttendanceReport(startDate?: string, endDate?: string): Observable<Blob> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get(`${this.apiUrl}/report/admin/download-report`, {
      params,
      responseType: 'blob'
    });
  }
  getAuditLogs(page: number = 1, pageSize: number = 20,
    filters?: { startDate?: string, endDate?: string, userName?: string, action?: string }
  ): Observable<PaginatedResult<AuditLogDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    if (filters?.userName) params = params.set('userName', filters.userName);
    if (filters?.action) params = params.set('action', filters.action);

    return this.http.get<PaginatedResult<AuditLogDto>>(`${this.apiUrl}/audit/logs`, { params });
  }

  downloadAuditLogs(filters?: { startDate?: string, endDate?: string, userName?: string, action?: string }
  ): Observable<Blob> {
    let params = new HttpParams();

    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    if (filters?.userName) params = params.set('userName', filters.userName);
    if (filters?.action) params = params.set('action', filters.action);

    return this.http.get(`${this.apiUrl}/audit/export`,
      {
        params,
        responseType: 'blob'
      }
    );
  }
  getAllUsers(): Observable<EmployeeDto[]> {
    return this.http.get<EmployeeDto[]>(`${this.apiUrl}/account/users`);
  }

  getUserById(id: string): Observable<EmployeeDto> {
    return this.http.get<EmployeeDto>(`${this.apiUrl}/account/users/${id}`);
  }

  updateUser(id: string, data: UpdateEmployeeDto): Observable<any> {
    const secureData = { ...data };

    if (secureData.Password && secureData.Password.trim() !== '') {
      secureData.Password = this.hashPassword(secureData.Password);
    }

    return this.http.put(`${this.apiUrl}/account/update/${id}`, secureData);
  }
  deleteUser(id: string): Observable<any> {
  return this.http.delete(`${this.apiUrl}/account/delete/${id}`);
}
}
