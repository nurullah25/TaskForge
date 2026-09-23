import { DatePipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { LabelChip } from '../../labels/label-chip/label-chip';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { isOverdue } from '../task.models';
import { TaskListItem } from '../task-search.service';
import { PriorityChip } from '../priority-chip/priority-chip';

// One row in a task list, used by search results and the dashboard.
@Component({
  selector: 'app-task-list-row',
  imports: [DatePipe, MatIconModule, LabelChip, UserAvatar, PriorityChip],
  template: `
    @let item = task();
    <button class="row" type="button" (click)="open.emit(item)">
      <app-priority-chip [priority]="item.priority" />

      <span class="key muted">{{ item.projectKey }}-{{ item.number }}</span>

      <span class="title" [class.done]="item.completedAt">{{ item.title }}</span>

      @for (label of item.labels; track label.id) {
        <app-label-chip [label]="label" />
      }

      <span class="column muted">{{ item.columnName }}</span>

      @if (item.dueDate) {
        <span class="due" [class.overdue]="overdue()">
          <mat-icon>event</mat-icon>
          {{ item.dueDate | date: 'MMM d' }}
        </span>
      }

      @if (item.assignee; as assignee) {
        <app-user-avatar [name]="assignee.fullName" [size]="24" />
      } @else {
        <span class="unassigned muted">Unassigned</span>
      }
    </button>
  `,
  styleUrl: './task-list-row.scss',
})
export class TaskListRow {
  readonly task = input.required<TaskListItem>();

  readonly open = output<TaskListItem>();

  protected readonly overdue = computed(() =>
    isOverdue(this.task().dueDate, !!this.task().completedAt),
  );
}
