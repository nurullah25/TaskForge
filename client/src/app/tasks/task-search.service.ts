import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Label } from '../labels/label.models';
import { PagedResult } from '../shared/models/paged-result';
import { ColumnCategory } from '../boards/board.models';
import { TaskMember, TaskPriority } from './task.models';

export type TaskSort = 'Newest' | 'DueDate' | 'Priority';

export interface TaskFilters {
  text?: string | null;
  assigneeId?: number | null;
  unassigned?: boolean;
  priority?: TaskPriority | null;
  category?: ColumnCategory | null;
  labelId?: number | null;
  dueFrom?: string | null;
  dueTo?: string | null;
  overdue?: boolean;
  sort?: TaskSort;
  page?: number;
  pageSize?: number;
}

export interface TaskListItem {
  id: number;
  number: number;
  projectKey: string;
  title: string;
  priority: TaskPriority;
  dueDate: string | null;
  assignee: TaskMember | null;
  labels: Label[];
  boardId: number;
  columnName: string;
  category: ColumnCategory;
  completedAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class TaskSearchService {
  private readonly http = inject(HttpClient);

  search(projectId: number, filters: TaskFilters): Observable<PagedResult<TaskListItem>> {
    let params = new HttpParams();

    // Empty values are left out so the URL only carries the filters actually in use.
    for (const [key, value] of Object.entries(filters)) {
      if (value !== null && value !== undefined && value !== '' && value !== false) {
        params = params.set(key, String(value));
      }
    }

    return this.http.get<PagedResult<TaskListItem>>(`/api/projects/${projectId}/tasks`, { params });
  }
}
