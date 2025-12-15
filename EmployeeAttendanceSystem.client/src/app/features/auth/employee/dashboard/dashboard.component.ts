import { Component, inject, OnInit, signal } from '@angular/core';
import { AuthService } from '../../../../core/services/auth.service';
import { AttendanceService } from '../../../../core/services/attendance.service';
import { finalize } from 'rxjs';
import { errorContext } from 'rxjs/internal/util/errorContext';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DailyAttendanceSummaryDto } from '../../../../core/models/daily-attendance-summary-dto';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit{

    summary = signal<DailyAttendanceSummaryDto | null>(null);

    ngOnInit(): void {
      this.loadSummary();
    }

    loadSummary(){
      this.attendanceService.getDailySummary().subscribe({
        next:(data) =>{
          this.summary.set(data);
        }
      })
    }

  
    public readonly authService = inject(AuthService);
    private readonly attendanceService = inject(AttendanceService);

    today = new Date();
    isProcessing = false;
    
    message: string | null = null; 
    isSuccess = false;

    private startProcess(){
      this.isProcessing = true;
      this.message = null; 
    }

    private showSuccess(msg: string){
      this.message = msg; 
      this.isSuccess = true;
      setTimeout(() => this.message = null, 5000);
    }

    private showError(msg: string){
      this.message = msg;
      this.isSuccess = false;
    }

    onCheckIn(){
      this.startProcess();

      this.attendanceService.checkIn().pipe(
        finalize(() => this.isProcessing = false)
      ).subscribe({
        next : (response: any) => {

          this.showSuccess(response.Message || "Check-in kaydedildi.");
          this.loadSummary();
        },
        error : (err) => {
          console.error(err);
          const serverError = err.error?.Message || err.error?.message;
          this.showError(serverError || "Check-in işlemi başarısız.");
        }
      });
    }

    onCheckOut(){
      this.startProcess();

      this.attendanceService.checkOut().pipe(
        finalize(() => this.isProcessing = false)
      ).subscribe({
        next : (response: any) => {
          this.showSuccess(response.Message || "Check-out kaydedildi.");
          this.loadSummary();
        },
        error : (err) => {
          const serverError = err.error?.Message || err.error?.message;
          this.showError(serverError || "Check-out işlemi başarısız");
        }
      });
    }
}