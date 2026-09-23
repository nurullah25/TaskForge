import { Component, inject } from '@angular/core';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Router } from '@angular/router';
import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';
import { AppNotification, NotificationService } from '../notification.service';

@Component({
  selector: 'app-notification-menu',
  imports: [
    MatMenuModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
    MatDividerModule,
    RelativeTimePipe,
  ],
  templateUrl: './notification-menu.html',
  styleUrl: './notification-menu.scss',
})
export class NotificationMenu {
  private readonly router = inject(Router);
  protected readonly notifications = inject(NotificationService);

  protected open(notification: AppNotification): void {
    this.notifications.markRead(notification);

    // Notifications about a task deep-link to it on its board.
    if (notification.boardId && notification.taskId) {
      this.router.navigate(['/boards', notification.boardId], {
        queryParams: { task: notification.taskId },
      });
    }
  }
}
