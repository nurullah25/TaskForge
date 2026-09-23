import { Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { TASK_PRIORITIES, TaskMember } from '../task.models';
import { PriorityChip } from '../priority-chip/priority-chip';

// The same fields are used when creating a task and when editing one, so they live
// in one component and the parent owns the form group.
@Component({
  selector: 'app-task-fields',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    UserAvatar,
    PriorityChip,
  ],
  templateUrl: './task-fields.html',
  styles: `
    .option {
      display: flex;
      align-items: center;
      gap: 8px;
    }
  `,
})
export class TaskFields {
  readonly form = input.required<FormGroup>();
  readonly members = input.required<TaskMember[]>();

  protected readonly priorities = TASK_PRIORITIES;
}
