import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../../core/services/admin.service';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-edit-employee',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './edit-employee.component.html',
  styleUrl: './edit-employee.component.scss'
})
export class EditEmployeeComponent implements OnInit {
  private fb = inject(FormBuilder);
  private adminService = inject(AdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  editForm: FormGroup;
  userId: string = '';
  isLoading = false;
  successMessage: string | null = null;
  errorMessage: string | null = null;

  constructor() {
    this.editForm = this.fb.group({
      Name: ['', Validators.required],
      Surname: ['', Validators.required],
      Email: ['', [Validators.required, Validators.email]],
      Department: [''],
      Password: [''] 
    });
  }

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id') || '';
    
    if (this.userId) {
      this.loadUserData();
    }
  }

  loadUserData() {
    this.isLoading = true;
    this.adminService.getUserById(this.userId).subscribe({
      next: (user) => {
        this.editForm.patchValue({
          Name: user.Name,
          Surname: user.Surname,
          Email: user.Email,
          Department: user.Department
        });
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = "Kullanıcı bilgileri yüklenemedi.";
        this.isLoading = false;
      }
    });
  }

  onUpdate() {
    if (this.editForm.invalid) return;

    this.isLoading = true;
    this.successMessage = null;
    this.errorMessage = null;

    const updateData = { ...this.editForm.value };

    if (!updateData.Password || updateData.Password.trim() === '') {
        delete updateData.Password; 
    }

    this.adminService.updateUser(this.userId, updateData).subscribe({
      next: (res) => {
        this.successMessage = res.message || "Kullanıcı başarıyla güncellendi.";
        this.isLoading = false;
        
        this.editForm.patchValue({ Password: '' });

        const currentUser = this.authService.currentUser();

        if(currentUser && currentUser.Email === updateData.Email){
          this.authService.refreshCurrentUser();
        }
      },
      error: (err) => {
        console.error("Güncelleme hatası:", err);
        this.isLoading = false;

        
        if (err.error?.message) {
            this.errorMessage = err.error.message;
        }

        else if (Array.isArray(err.error)) {
            this.errorMessage = err.error.map((e: any) => e.description || e.code).join(', ');
        }
        else {
            this.errorMessage = "Güncelleme başarısız oldu.";
        }
      }
    });
  }
}