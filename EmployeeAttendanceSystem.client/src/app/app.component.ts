import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'EmployeeAttendanceSystem.client';
  public authService = inject(AuthService);

  now = new Date();

  private timerId: any;

  ngOnInit(): void {

    this.timerId = setInterval(() => {
      this.now = new Date();
    }, 1000);
  }

  ngOnDestroy(): void {

    if (this.timerId) {
      clearInterval(this.timerId);
    }
  }

  logout(): void {
    this.authService.logout().subscribe();
  }
}