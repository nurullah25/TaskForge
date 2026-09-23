import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, EMPTY, filter, finalize, Observable, switchMap } from 'rxjs';
import { getErrorMessage } from '../../core/http/api-error';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { TaskDetails, TaskMember, taskKey } from '../task.models';
import { TaskService } from '../task.service';
import { createTaskForm, toDateOnly } from '../task-form';
import { TaskFields } from '../task-fields/task-fields';

export interface TaskPanelData {
  taskId: number;
  members: TaskMember[];
}

// What happened to the task while the panel was open, so the board can update itself.
export type TaskPanelResult = { changed: TaskDetails } | { deleted: number } | undefined;

@Component({
  selector: 'app-task-detail-panel',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    UserAvatar,
    TaskFields,
  ],
  templateUrl: './task-detail-panel.html',
  styleUrl: './task-detail-panel.scss',
})
export class TaskDetailPanel {
  private readonly api = inject(TaskService);
  private readonly dialog = inject(MatDialog);
  private readonly dialogRef = inject(MatDialogRef<TaskDetailPanel, TaskPanelResult>);
  private readonly data = inject<TaskPanelData>(MAT_DIALOG_DATA);

  protected readonly members = this.data.members;
  protected readonly task = signal<TaskDetails | null>(null);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = createTaskForm();

  protected readonly key = computed(() => {
    const task = this.task();
    return task ? taskKey(task.projectKey, task.number) : '';
  });
  protected readonly canEdit = computed(() => this.task()?.myRole !== 'Viewer');

  static open(dialog: MatDialog, data: TaskPanelData): Observable<TaskPanelResult> {
    return dialog
      .open(TaskDetailPanel, {
        data,
        panelClass: 'task-panel',
        position: { right: '0', top: '0' },
        width: '520px',
        maxWidth: '100vw',
        height: '100vh',
        autoFocus: false,
        // Closed through close() instead, so the board hears about unsaved edits.
        disableClose: true,
      })
      .afterClosed();
  }

  constructor() {
    this.dialogRef.backdropClick().subscribe(() => this.close());
    this.dialogRef.keydownEvents().subscribe((event) => {
      if (event.key === 'Escape') {
        this.close();
      }
    });

    this.api
      .get(this.data.taskId)
      .pipe(
        catchError(() => {
          this.dialogRef.close();
          return EMPTY;
        }),
      )
      .subscribe((task) => this.setTask(task));
  }

  protected save(): void {
    const task = this.task();
    if (!task || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.api
      .update(task.id, {
        title: value.title,
        description: value.description || null,
        priority: value.priority,
        assigneeId: value.assigneeId,
        dueDate: toDateOnly(value.dueDate),
        // Sent back so the server can tell whether someone else saved in the meantime.
        rowVersion: task.rowVersion,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => this.setTask(updated),
        error: (error) => this.errorMessage.set(getErrorMessage(error)),
      });
  }

  protected reload(): void {
    this.errorMessage.set(null);
    this.api.get(this.data.taskId).subscribe((task) => this.setTask(task));
  }

  protected deleteTask(): void {
    const task = this.task()!;

    confirmAction(this.dialog, {
      title: `Delete ${this.key()}?`,
      message: 'The task and its comments and attachments are deleted permanently.',
      confirmLabel: 'Delete task',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(task.id)),
      )
      .subscribe(() => this.dialogRef.close({ deleted: task.id }));
  }

  protected close(): void {
    const task = this.task();
    this.dialogRef.close(task ? { changed: task } : undefined);
  }

  private setTask(task: TaskDetails): void {
    this.task.set(task);
    this.form.reset({
      title: task.title,
      description: task.description ?? '',
      priority: task.priority,
      assigneeId: task.assignee?.id ?? null,
      dueDate: task.dueDate ? new Date(task.dueDate) : null,
    });

    if (!this.canEdit()) {
      this.form.disable();
    }
  }
}
