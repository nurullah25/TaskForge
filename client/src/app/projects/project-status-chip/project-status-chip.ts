import { Component, computed, input } from '@angular/core';
import { PROJECT_STATUSES, ProjectStatus } from '../project.models';

@Component({
  selector: 'app-project-status-chip',
  template: `<span [class]="'chip ' + status().toLowerCase()">{{ label() }}</span>`,
  styles: `
    .chip {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 500;
      white-space: nowrap;
    }

    .active {
      background: #dcfce7;
      color: #166534;
    }

    .onhold {
      background: #fef3c7;
      color: #92400e;
    }

    .completed {
      background: #dbeafe;
      color: #1e40af;
    }

    .archived {
      background: #e5e7eb;
      color: #374151;
    }
  `,
})
export class ProjectStatusChip {
  readonly status = input.required<ProjectStatus>();

  protected readonly label = computed(
    () => PROJECT_STATUSES.find((s) => s.value === this.status())?.label ?? this.status(),
  );
}
