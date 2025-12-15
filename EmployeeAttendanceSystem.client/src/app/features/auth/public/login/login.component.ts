import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  loginForm: FormGroup;
  errorMessage: string | null = null;
  isLoading = false;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.errorMessage = 'Lütfen tüm alanları doldurun.';
      return;
    }
    
    this.errorMessage = null;
    this.isLoading = true;
    this.loginForm.disable();

    this.authService.login(this.loginForm.value).subscribe({
      next: (user) => {
        const target = user.Role === 'Admin' ? '/dashboard' : '/dashboard';
        this.router.navigate([target]);
      },
      error: (err) => {
        
        this.loginForm.enable();
        this.isLoading = false;

        if (err.status === 401) {
          this.errorMessage = 'E-posta adresi veya şifre hatalı.';
          return;
        }

        if (err.error && typeof err.error === 'object' && err.error.message) {
          this.errorMessage = err.error.message;
        }
        else if (typeof err.error === 'string') {
          this.errorMessage = err.error;
        }
        else {
          this.errorMessage = 'Giriş yapılamadı. Lütfen bilgilerinizi kontrol edin.';
        }
      } 
    });
  }
}