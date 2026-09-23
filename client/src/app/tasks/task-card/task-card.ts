import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { LabelChip } from '../../labels/label-chip/label-chip';
import { isOverdue, TaskCard, taskKey } from '../task.models';
import { PriorityChip } from '../priority-chip/priority-chip';

@Component({
  selector: 'app-task-card',
  imports: [DatePipe, MatIconModule, UserAvatar, PriorityChip, LabelChip],
  template: `
    @let card = task();
    <article class="card">
      @if (card.labels.length) {
        <div class="labels">
          @for (label of card.labels; track label.id) {
            <app-label-chip [label]="label" />
          }
        </div>
      }

      <p class="title">{{ card.title }}</p>

      <div class="meta">
        <span class="key muted">{{ key() }}</span>
        <app-priority-chip [priority]="card.priority" />

        @if (card.dueDate) {
          <span class="due" [class.overdue]="overdue()">
            <mat-icon>event</mat-icon>
            {{ card.dueDate | date: 'MMM d' }}
          </span>
        }

        <span class="icons muted">
          @if (card.hasDescription) {
            <mat-icon title="Has a description">notes</mat-icon>
          }
          @if (card.commentCount) {
            <span class="comments" [title]="card.commentCount + ' comments'">
              <mat-icon>chat_bubble</mat-icon>
              {{ card.commentCount }}
            </span>
          }
        </span>

        @if (card.assignee; as assignee) {
          <app-user-avatar class="assignee" [name]="assignee.fullName" [size]="24" />
        }
      </div>
    </article>
  `,
  styleUrl: './task-card.scss',
})
export class TaskCardComponent {
  readonly task = input.required<TaskCard>();
  readonly projectKey = input.required<string>();

  protected readonly key = computed(() => taskKey(this.projectKey(), this.task().number));
  protected readonly overdue = computed(() => isOverdue(this.task().dueDate));
}
