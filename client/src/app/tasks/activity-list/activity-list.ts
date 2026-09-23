import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';
import { ActivityEntry, describeActivity } from '../activity.service';

// Used by the task panel (history of one task) and the project page (recent activity).
@Component({
  selector: 'app-activity-list',
  imports: [MatIconModule, RouterLink, UserAvatar, RelativeTimePipe],
  template: `
    <ul class="entries">
      @for (entry of entries(); track entry.id) {
        <li>
          <app-user-avatar [name]="entry.user.fullName" [size]="28" />
          <div class="text">
            <span>
              <strong>{{ entry.user.fullName }}</strong>
              {{ describe(entry) }}
              @if (showTask() && entry.taskNumber) {
                on
                <a [routerLink]="[]" [queryParams]="{ task: entry.taskId }">
                  {{ projectKey() }}-{{ entry.taskNumber }}
                </a>
              }
            </span>
            <span class="muted when">{{ entry.createdAt | relativeTime }}</span>
          </div>
        </li>
      } @empty {
        <li class="muted empty">Nothing has happened here yet.</li>
      }
    </ul>
  `,
  styles: `
    .entries {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    li {
      display: flex;
      gap: 10px;
      padding: 8px 0;
    }

    .text {
      display: flex;
      flex-direction: column;
      font-size: 14px;
    }

    .when {
      font-size: 12px;
    }

    .empty {
      font-size: 14px;
    }
  `,
})
export class ActivityList {
  readonly entries = input.required<ActivityEntry[]>();
  readonly showTask = input(false);
  readonly projectKey = input('');

  protected readonly describe = describeActivity;
}
