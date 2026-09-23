import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TaskPriority } from '../task.models';

const ICONS: Record<TaskPriority, string> = {
  Low: 'keyboard_double_arrow_down',
  Medium: 'drag_handle',
  High: 'keyboard_arrow_up',
  Urgent: 'keyboard_double_arrow_up',
};

@Component({
  selector: 'app-priority-chip',
  imports: [MatIconModule],
  template: `
    <span [class]="'priority ' + priority().toLowerCase()" [attr.title]="priority() + ' priority'">
      <mat-icon>{{ icon() }}</mat-icon>
      @if (showLabel()) {
        {{ priority() }}
      }
    </span>
  `,
  styles: `
    .priority {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      font-weight: 500;
    }

    mat-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
    }

    .low {
      color: #0891b2;
    }

    .medium {
      color: #64748b;
    }

    .high {
      color: #ea580c;
    }

    .urgent {
      color: #dc2626;
    }
  `,
})
export class PriorityChip {
  readonly priority = input.required<TaskPriority>();
  readonly showLabel = input(false);

  protected icon(): string {
    return ICONS[this.priority()];
  }
}
