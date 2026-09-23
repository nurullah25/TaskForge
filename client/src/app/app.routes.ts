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
      {
        path: 'projects',
        title: 'Projects · TaskForge',
        loadComponent: () =>
          import('./projects/project-list-page/project-list-page').then((m) => m.ProjectListPage),
      },
      {
        path: 'projects/:projectId',
        title: 'Project · TaskForge',
        loadComponent: () =>
          import('./projects/project-page/project-page').then((m) => m.ProjectPage),
      },
      {
        path: 'projects/:projectId/boards/:boardId',
        title: 'Board · TaskForge',
        loadComponent: () => import('./boards/board-page/board-page').then((m) => m.BoardPage),
      },
      {
        path: 'projects/:projectId/settings',
        title: 'Project settings · TaskForge',
        loadComponent: () =>
          import('./projects/project-settings-page/project-settings-page').then(
            (m) => m.ProjectSettingsPage,
          ),
      },
      {
        path: 'organizations/:organizationId/settings',
        title: 'Organization settings · TaskForge',
        loadComponent: () =>
          import('./organizations/organization-settings-page/organization-settings-page').then(
            (m) => m.OrganizationSettingsPage,
          ),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', title: 'Page not found · TaskForge', component: NotFoundPage },
];
