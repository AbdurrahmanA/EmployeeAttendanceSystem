import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../../core/services/admin.service';
import { EmployeeDto } from '../../../../core/models/update-employee-dto';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {
  private adminService = inject(AdminService);

  users = signal<EmployeeDto[]>([]);
  isLoading = signal(false);

  userToDelete: EmployeeDto | null = null;

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading.set(true);
    this.adminService.getAllUsers().subscribe({
      next: (data) => {
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  onDeleteClick(user: EmployeeDto) {
    this.userToDelete = user;
  }

  cancelDelete() {
    this.userToDelete = null; 
  }

  confirmDelete() {
    if (!this.userToDelete) return;

    this.isLoading.set(true);
    
    this.adminService.deleteUser(this.userToDelete.Id).subscribe({
      next: () => {
        this.loadUsers();
        this.userToDelete = null;
      },
      error: (err) => {
        console.error(err);
        alert('Silme işlemi başarısız oldu.');
        this.isLoading.set(false);
        this.userToDelete = null;
      }
    });
  }
}