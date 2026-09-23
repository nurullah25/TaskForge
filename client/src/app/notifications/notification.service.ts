import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../core/auth/auth.service';
import { RealtimeService } from '../core/realtime/realtime.service';
import { PagedResult } from '../shared/models/paged-result';

export type NotificationType = 'TaskAssigned' | 'CommentAdded' | 'AddedToProject';

export interface AppNotification {
  id: number;
  type: NotificationType;
  message: string;
  taskId: number | null;
  boardId: number | null;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly realtime = inject(RealtimeService);

  private readonly items = signal<AppNotification[]>([]);
  readonly notifications = this.items.asReadonly();
  readonly unreadCount = computed(() => this.items().filter((n) => !n.isRead).length);

  private readonly userId = computed(() => this.auth.currentUser()?.id ?? null);

  constructor() {
    effect(() => {
      const userId = this.userId();
      untracked(() => (userId === null ? this.items.set([]) : this.load()));
    });

    // Catch up on anything missed while the connection was down.
    this.realtime.reconnected$.pipe(takeUntilDestroyed()).subscribe(() => this.load());

    // New notifications arrive over the hub while the app is open.
    this.realtime.notifications$
      .pipe(takeUntilDestroyed())
      .subscribe((notification) =>
        this.items.update((list) => [notification as AppNotification, ...list]),
      );
  }

  load(): void {
    this.http
      .get<PagedResult<AppNotification>>('/api/notifications?page=1&pageSize=20')
      .subscribe((result) => this.items.set(result.items));
  }

  markRead(notification: AppNotification): void {
    if (notification.isRead) {
      return;
    }

    this.setRead([notification.id]);
    this.http.post(`/api/notifications/${notification.id}/read`, null).subscribe();
  }

  markAllRead(): void {
    this.setRead(this.items().map((n) => n.id));
    this.http.post('/api/notifications/read-all', null).subscribe();
  }

  private setRead(ids: number[]): void {
    this.items.update((list) => list.map((n) => (ids.includes(n.id) ? { ...n, isRead: true } : n)));
  }
}
