import { Routes } from '@angular/router';

import { LoginComponent } from './features/auth/public/login/login.component';
import { DashboardComponent } from './features/auth/employee/dashboard/dashboard.component';

import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { RegisterEmployeeComponent } from './features/auth/admin/register-employee/register-employee.component';
import { ReportAttendanceComponent } from './features/auth/admin/report-attendance/report-attendance.component';
import { ReportAuditComponent } from './features/auth/admin/report-audit/report-audit.component';
import { UserListComponent } from './features/auth/admin/user-list/user-list.component';
import { EditEmployeeComponent } from './features/auth/admin/edit-employee/edit-employee.component';
import { ProfileComponent } from './features/auth/employee/profile/profile.component';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  { path: 'login', component: LoginComponent },

  {
    path: 'admin/register',
    component: RegisterEmployeeComponent,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'profile',
    component: ProfileComponent,
    canActivate: [authGuard]
  },
  {
    path: 'api/report/admin/all-logs',
    component: ReportAttendanceComponent,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'api/report/audit/logs',
    component: ReportAuditComponent,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/users',
    component: UserListComponent,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'admin/users/edit/:id',
    component: EditEmployeeComponent,
    canActivate: [authGuard, adminGuard]
  },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  {
    path: 'admin',
    component: DashboardComponent,
    canActivate: [authGuard, adminGuard]
  }
];