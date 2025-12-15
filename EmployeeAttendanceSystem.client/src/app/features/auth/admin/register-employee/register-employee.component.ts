import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

export const strongPasswordValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value || '';
  if (!value) return null;

  const hasUpper = /[A-Z]/.test(value);
  const hasLower = /[a-z]/.test(value);
  const hasNumeric = /[0-9]/.test(value);
  const hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value);

  const errors: ValidationErrors = {};
  if (!hasUpper) errors['missingUpper'] = true;
  if (!hasLower) errors['missingLower'] = true;
  if (!hasNumeric) errors['missingNumeric'] = true;
  if (!hasSpecial) errors['missingSpecial'] = true;

  return Object.keys(errors).length > 0 ? errors : null;
};

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (password && confirmPassword && password.value !== confirmPassword.value) {
    confirmPassword.setErrors({ passwordMismatch: true });
    return { passwordMismatch: true };
  }
  if (confirmPassword?.hasError('passwordMismatch')) {
    confirmPassword.setErrors(null);
  }
  return null;
};

@Component({
  selector: 'app-register-employee',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register-employee.component.html',
  styleUrl: './register-employee.component.scss'
})
export class RegisterEmployeeComponent {
  
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  registerForm: FormGroup;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  isSubmitting = false;

  constructor() {
    this.registerForm = this.fb.group({
      name: ['', [Validators.required]],
      surname: ['', [Validators.required]],
      department: [''], 
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6), strongPasswordValidator]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: passwordMatchValidator
    });
  }

  onRegister() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.errorMessage = 'Lütfen formu kontrol ediniz.';
      return;
    }

    this.errorMessage = null;
    this.successMessage = null;
    this.isSubmitting = true;
    this.registerForm.disable(); 

    const { confirmPassword, ...registerDto } = this.registerForm.value;

    this.authService.register(registerDto).subscribe({
      next: (response) => {
        console.log('Kayıt başarılı:', response);
        this.successMessage = response.Message || 'Personel başarıyla kaydedildi.';
        
        this.isSubmitting = false;
        this.registerForm.enable(); 
        this.registerForm.reset(); 
      },
      error: (err) => {
        console.error('Kayıt hatası:', err);
        this.isSubmitting = false;
        this.registerForm.enable(); 
        
        if (Array.isArray(err.error)) {
             this.errorMessage = err.error
                .map((e: any) => e.description || e.message || JSON.stringify(e))
                .join(', ');
        }
        else if (err.error?.message) {
            this.errorMessage = err.error.message;
        }
        else {
            this.errorMessage = 'Kayıt işlemi başarısız oldu. Lütfen bilgileri kontrol edin.';
        }
      }
    });
  }
}