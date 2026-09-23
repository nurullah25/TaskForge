import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable } from 'rxjs';
import { getErrorMessage } from '../../core/http/api-error';
import { TaskDetails, TaskMember } from '../task.models';
import { TaskService } from '../task.service';
import { createTaskForm, toDateOnly } from '../task-form';
import { TaskFields } from '../task-fields/task-fields';

export interface TaskCreateDialogData {
  columnId: number;
  columnName: string;
  members: TaskMember[];
}

@Component({
  selector: 'app-task-create-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatIconModule, TaskFields],
  template: `
    <h2 mat-dialog-title>New task in {{ data.columnName }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        @if (errorMessage(); as message) {
          <div class="form-error" role="alert"><mat-icon>error</mat-icon>{{ message }}</div>
        }
        <app-task-fields [form]="form" [members]="data.members" />
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">Create task</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class TaskCreateDialog {
  private readonly api = inject(TaskService);
  private readonly dialogRef = inject(MatDialogRef<TaskCreateDialog, TaskDetails>);
  protected readonly data = inject<TaskCreateDialogData>(MAT_DIALOG_DATA);

  protected readonly form = createTaskForm();
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  static open(dialog: MatDialog, data: TaskCreateDialogData): Observable<TaskDetails | undefined> {
    return dialog.open(TaskCreateDialog, { width: '560px', data }).afterClosed();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.api
      .create({
        columnId: this.data.columnId,
        title: value.title,
        description: value.description || null,
        priority: value.priority,
        assigneeId: value.assigneeId,
        dueDate: toDateOnly(value.dueDate),
      })
      .subscribe({
        next: (task) => this.dialogRef.close(task),
        error: (error) => {
          this.saving.set(false);
          this.errorMessage.set(getErrorMessage(error));
        },
      });
  }
}
