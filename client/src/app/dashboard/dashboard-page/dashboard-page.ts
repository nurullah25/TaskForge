import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';
import { PageHeader } from '../../shared/components/page-header/page-header';

// Placeholder until the dashboard statistics are built.
@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, MatCardModule, MatIconModule, PageHeader],
  template: `
    <app-page-header [title]="greeting()" [subtitle]="(today | date: 'EEEE, MMMM d') ?? ''" />

    <mat-card appearance="outlined" class="empty">
      <mat-icon>insights</mat-icon>
      <h2>Your overview will show up here</h2>
      <p class="muted">Projects, open tasks and deadlines will be summarised on this page.</p>
    </mat-card>
  `,
  styles: `
    .empty {
      align-items: center;
      padding: 48px 24px;
      text-align: center;
      background: var(--mat-sys-surface);
    }

    mat-icon {
      font-size: 40px;
      width: 40px;
      height: 40px;
      color: var(--mat-sys-primary);
    }

    h2 {
      margin: 12px 0 4px;
      font-size: 18px;
      font-weight: 600;
    }

    p {
      margin: 0;
    }
  `,
})
export class DashboardPage {
  private readonly auth = inject(AuthService);

  protected readonly today = new Date();

  protected readonly greeting = computed(() => {
    const hour = this.today.getHours();
    const partOfDay = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    const firstName = this.auth.currentUser()?.fullName.split(' ')[0] ?? '';
    return `Good ${partOfDay}, ${firstName}`;
  });
}
