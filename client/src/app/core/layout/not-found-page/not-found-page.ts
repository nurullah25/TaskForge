import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [MatButtonModule, RouterLink],
  template: `
    <section class="not-found">
      <p class="code">404</p>
      <h1>Page not found</h1>
      <p class="muted">The page you're looking for doesn't exist or has been moved.</p>
      <a mat-flat-button routerLink="/">Back to TaskForge</a>
    </section>
  `,
  styles: `
    .not-found {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 16px;
      text-align: center;
    }

    .code {
      margin: 0;
      font-size: 56px;
      font-weight: 700;
      color: var(--mat-sys-primary);
    }

    h1 {
      margin: 8px 0;
    }

    p.muted {
      margin: 0 0 24px;
    }
  `,
})
export class NotFoundPage {}
