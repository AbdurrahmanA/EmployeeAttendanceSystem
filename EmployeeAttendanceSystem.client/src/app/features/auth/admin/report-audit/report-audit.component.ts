import { Component, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { AdminService } from '../../../../core/services/admin.service';
import { AuditLogDto } from '../../../../core/models/audit-log.dto';
import { CommonModule, DatePipe } from '@angular/common';
import { ShortenPipe } from '../../../../core/pipes/shorten.pipe';

@Component({
  selector: 'app-report-audit',
  imports: [CommonModule, DatePipe, ShortenPipe],
  templateUrl: './report-audit.component.html',
  styleUrl: './report-audit.component.scss'
})
export class ReportAuditComponent implements OnInit {

  private adminService = inject(AdminService);

  startDate = signal<string>('');
  endDate = signal<string>('');
  userName = signal<string>('');
  action = signal<string>('');

  logs = signal<AuditLogDto[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  currentPage = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(0);

  ngOnInit(): void {
    this.loadLogs();
  }

  onFilterChange(event: Event, targetSignal: WritableSignal<string>) {
    const inputElement = event.target as HTMLInputElement | HTMLSelectElement;
    targetSignal.set(inputElement.value);
  }
  loadLogs() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const filters = {
      startDate: this.startDate() || undefined,
      endDate: this.endDate() || undefined,
      userName: this.userName() || undefined,
      action: this.action() || undefined
    };

    this.adminService.getAuditLogs(this.currentPage(), this.pageSize(), filters).subscribe({
      next: (response) => {
        this.logs.set(response.data || []);
        this.totalCount.set(response.totalCount || 0);
        this.totalPages.set(response.totalPages || 0);

        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set('Loglar yüklenirken bir hata oluştu.');
        this.logs.set([]);
        this.isLoading.set(false);
      }
    });
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.currentPage.set(newPage);
      this.loadLogs();
    }
  }

  onFilter() {
    this.currentPage.set(1);
    this.loadLogs();
  }

  downloadReport() {
    this.isLoading.set(true);
    const filters = {
      startDate: this.startDate() || undefined,
      endDate: this.endDate() || undefined,
      userName: this.userName() || undefined,
      action: this.action() || undefined
    };

    this.adminService.downloadAuditLogs(filters).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `DenetimLoglari_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Audit log indirme hatası:', err);
        this.errorMessage.set('Dosya indirilemedi.');
        this.isLoading.set(false);
      }
    });
  }
}