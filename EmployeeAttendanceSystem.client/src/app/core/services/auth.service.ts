import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment.development';
import { UserDto } from '../models/user-dto';
import { finalize, Observable, tap } from 'rxjs';
import { RegisterDto } from '../models/register-dto';
import { LoginDto } from '../models/login-dto';

const USER_KEY = 'auth-user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private http = inject(HttpClient);
  private router = inject(Router);

  private apiUrl = environment.apiUrl + '/api/account';

  public currentUser = signal<UserDto | null>(null);
  public isLoggedIn = computed(() => this.currentUser() != null);
  public isAdmin = computed(() => this.currentUser()?.Role === 'Admin');

  constructor() {
    const userJson = localStorage.getItem(USER_KEY);
    if (userJson) {
      const user = JSON.parse(userJson) as UserDto;
      this.currentUser.set(user);
    }
  }

  refreshCurrentUser(): void{

    this.http.get<any>(`${this.apiUrl}/profile`).subscribe({
      next:(profileData) => {
        const currentUser = this.currentUser();

        if(currentUser){
          const updatedUser : UserDto ={
            ...currentUser,
            Name:profileData.Name,
            Surname: profileData.Surname,
            Email : profileData.Email,
            Department : profileData.Department
          };
          this.currentUser.set(updatedUser);
          localStorage.setItem(USER_KEY, JSON.stringify(updatedUser));
        }
      },
      error : (err) => console.error('Profil yenilenemedi',err)
    });

  }

  login(loginDto: LoginDto): Observable<UserDto> {


    return this.http.post<UserDto>(`${this.apiUrl}/login`, loginDto).pipe(
      tap((user: UserDto) => {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
      })
    );
  }


  register(registerDto: RegisterDto): Observable<any> {

    return this.http.post(`${this.apiUrl}/register`, registerDto);
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      finalize(() => {
        localStorage.removeItem(USER_KEY);
        this.currentUser.set(null);
        this.router.navigate(['/login']);
      })
    );
  }

  getToken(): string | null {
    return this.currentUser()?.Token ?? null;
  }
  getProfile(): Observable<any> {
    return this.http.get(`${this.apiUrl}/profile`);
  }
  changePassword(data: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, data);
  }
}