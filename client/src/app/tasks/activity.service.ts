import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../shared/models/paged-result';
import { TaskMember } from './task.models';

export type ActivityType =
  | 'TaskCreated'
  | 'TaskAssigned'
  | 'StatusChanged'
  | 'PriorityChanged'
  | 'DueDateChanged'
  | 'TitleChanged'
  | 'CommentAdded'
  | 'AttachmentAdded'
  | 'TaskDeleted';

export interface ActivityEntry {
  id: number;
  type: ActivityType;
  user: TaskMember;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
  taskId: number | null;
  taskNumber: number | null;
  taskTitle: string | null;
}

@Injectable({ providedIn: 'root' })
export class ActivityService {
  private readonly http = inject(HttpClient);

  forTask(taskId: number): Observable<ActivityEntry[]> {
    return this.http.get<ActivityEntry[]>(`/api/tasks/${taskId}/activity`);
  }

  forProject(projectId: number, page = 1, pageSize = 20): Observable<PagedResult<ActivityEntry>> {
    return this.http.get<PagedResult<ActivityEntry>>(
      `/api/projects/${projectId}/activity?page=${page}&pageSize=${pageSize}`,
    );
  }
}

// Turns a history row into a sentence, e.g. "changed priority from Low to High".
export function describeActivity(entry: ActivityEntry): string {
  const from = entry.oldValue;
  const to = entry.newValue;

  switch (entry.type) {
    case 'TaskCreated':
      return 'created this task';
    case 'TaskAssigned':
      if (!to) {
        return 'removed the assignee';
      }
      return from ? `reassigned this from ${from} to ${to}` : `assigned this to ${to}`;
    case 'StatusChanged':
      return `moved this from ${from} to ${to}`;
    case 'PriorityChanged':
      return `changed priority from ${from} to ${to}`;
    case 'DueDateChanged':
      if (!to) {
        return 'removed the due date';
      }
      return from ? `changed the due date from ${from} to ${to}` : `set the due date to ${to}`;
    case 'TitleChanged':
      return `renamed this from "${from}" to "${to}"`;
    case 'CommentAdded':
      return 'commented';
    case 'AttachmentAdded':
      return `attached ${to}`;
    case 'TaskDeleted':
      return `deleted ${to}`;
  }
}
