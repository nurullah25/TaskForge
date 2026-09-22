import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { NotFoundPage } from './core/layout/not-found-page/not-found-page';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Sign in · TaskForge',
    loadComponent: () => import('./auth/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'Create account · TaskForge',
    loadComponent: () => import('./auth/register-page/register-page').then((m) => m.RegisterPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'dashboard',
        title: 'Dashboard · TaskForge',
        loadComponent: () =>
          import('./dashboard/dashboard-page/dashboard-page').then((m) => m.DashboardPage),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', title: 'Page not found · TaskForge', component: NotFoundPage },
];
