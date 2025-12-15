import { Component, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { AdminService } from '../../../../core/services/admin.service';

@Component({
  selector: 'app-report-attendance',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe], 
  templateUrl: './report-attendance.component.html',
  styleUrl: './report-attendance.component.scss'
})
export class ReportAttendanceComponent implements OnInit {

  private adminService = inject(AdminService);

  startDate = signal<string>('');
  endDate = signal<string>('');
  
  logs = signal<any[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadLogs();
  }

  onFilterChange(event: Event, targetSignal: WritableSignal<string>) {
    const inputElement = event.target as HTMLInputElement;
    targetSignal.set(inputElement.value);
  }

  loadLogs() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const start = this.startDate() || undefined;
    const end = this.endDate() || undefined;

    this.adminService.getAttendanceLogs(start, end).subscribe({
      next: (data) => {
        this.logs.set(data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Rapor hatası:', err);
        this.errorMessage.set('Veriler yüklenirken bir hata oluştu.');
        this.isLoading.set(false);
      }
    });
  }

  downloadReport() {
    this.isLoading.set(true);
    
    const start = this.startDate() || undefined;
    const end = this.endDate() || undefined;

    this.adminService.downloadAttendanceReport(start, end).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `MesaiRaporu_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('İndirme hatası: ', err);
        this.errorMessage.set('Rapor indirilemedi');
        this.isLoading.set(false);
      }
    });
  }
}