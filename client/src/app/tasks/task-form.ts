import { FormControl, FormGroup, Validators } from '@angular/forms';
import { TaskDetails, TaskPriority } from './task.models';

export interface TaskFormValue {
  title: string;
  description: string;
  priority: TaskPriority;
  assigneeId: number | null;
  dueDate: Date | null;
}

export type TaskForm = FormGroup<{
  title: FormControl<string>;
  description: FormControl<string>;
  priority: FormControl<TaskPriority>;
  assigneeId: FormControl<number | null>;
  dueDate: FormControl<Date | null>;
}>;

export function createTaskForm(task?: TaskDetails): TaskForm {
  return new FormGroup({
    title: new FormControl(task?.title ?? '', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    description: new FormControl(task?.description ?? '', { nonNullable: true }),
    priority: new FormControl<TaskPriority>(task?.priority ?? 'Medium', { nonNullable: true }),
    assigneeId: new FormControl<number | null>(task?.assignee?.id ?? null),
    dueDate: new FormControl<Date | null>(task?.dueDate ? new Date(task.dueDate) : null),
  });
}

// The API stores due dates as plain dates, so only the calendar day is sent.
export function toDateOnly(value: Date | null): string | null {
  if (!value) {
    return null;
  }

  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${value.getFullYear()}-${month}-${day}`;
}
