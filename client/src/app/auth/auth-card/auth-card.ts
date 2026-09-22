import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

// Shared frame for the login and register pages.
@Component({
  selector: 'app-auth-card',
  imports: [MatCardModule, MatIconModule],
  template: `
    <div class="auth-page">
      <div class="brand">
        <span class="logo"><mat-icon>view_kanban</mat-icon></span>
        <span>TaskForge</span>
      </div>

      <mat-card appearance="outlined" class="card">
        <h1>{{ title() }}</h1>
        <p class="muted subtitle">{{ subtitle() }}</p>
        <ng-content />
      </mat-card>

      <div class="footer">
        <ng-content select="[authFooter]" />
      </div>
    </div>
  `,
  styles: `
    .auth-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 24px 16px;
      box-sizing: border-box;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 24px;
      font-size: 22px;
      font-weight: 700;
    }

    .logo {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: var(--mat-sys-primary);
      color: var(--mat-sys-on-primary);
    }

    .card {
      width: 100%;
      max-width: 420px;
      padding: 32px;
      box-sizing: border-box;
      background: var(--mat-sys-surface);
    }

    h1 {
      margin: 0;
      font-size: 22px;
      font-weight: 600;
    }

    .subtitle {
      margin: 6px 0 24px;
    }

    .footer {
      margin-top: 20px;
      font-size: 14px;
    }

    @media (max-width: 599px) {
      .card {
        padding: 24px 20px;
      }
    }
  `,
})
export class AuthCard {
  readonly title = input.required<string>();
  readonly subtitle = input('');
}
