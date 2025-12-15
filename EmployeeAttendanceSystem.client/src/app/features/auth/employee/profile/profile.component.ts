import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { groupBy } from 'rxjs';

@Component({
  selector: 'app-profile',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit{
  private fb = inject(FormBuilder);
  public authService = inject(AuthService);

  userProfile = signal<any>(null);

  passwordForm: FormGroup;

  isLoading = signal(false);
  message = signal<string | null>(null);
  isSuccess = signal(false);

  constructor() {
    this.passwordForm = this.fb.group({
      OldPassword: ['', Validators.required],
      NewPassword: ['', Validators.required, Validators.minLength(6)],
      ConfirmNewPassword: ['', Validators.required]
    },
      {
        validators: (group: FormGroup) => {
          const pass = group.get('NewPassword')?.value;
          const confirm = group.get('ConfirmNewPassword')?.value;
          return pass === confirm ? null : { mismatch: true }
        }
      });
  }
  ngOnInit() {
    this.loadProfile();
  }
  loadProfile(){
    this.authService.getProfile().subscribe({
      next:(data) => this.userProfile.set(data),
      error:(err) => console.error('Profil yüklenemedi', err)
    });
  }
  onChangePassword(){
    if(this.passwordForm.invalid) return;

    this.isLoading.set(true);
    this.message.set(null);

    this.authService.changePassword(this.passwordForm.value).subscribe({
      next:(res) =>{
        this.isSuccess.set(true);
        this.message.set(res.Message || 'Şifreniz değiştirildi');
        this.passwordForm.reset();
        this.isLoading.set(false);
      },
       error: (err) => {
        this.isSuccess.set(false);
        if (Array.isArray(err.error)) {
            this.message.set(err.error.map((e: any) => e.description).join(', '));
        } else {
            this.message.set(err.error?.Message || 'Şifre değiştirilemedi.');
        }
        this.isLoading.set(false);
      }
    });
  }
}
