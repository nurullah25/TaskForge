import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult } from '../shared/models/paged-result';
import { TaskMember } from './task.models';

export interface TaskComment {
  id: number;
  author: TaskMember;
  body: string;
  createdAt: string;
  updatedAt: string | null;
  canEdit: boolean;
  canDelete: boolean;
}

@Injectable({ providedIn: 'root' })
export class CommentService {
  private readonly http = inject(HttpClient);

  list(taskId: number, page: number, pageSize = 20): Observable<PagedResult<TaskComment>> {
    return this.http.get<PagedResult<TaskComment>>(
      `/api/tasks/${taskId}/comments?page=${page}&pageSize=${pageSize}`,
    );
  }

  add(taskId: number, body: string): Observable<TaskComment> {
    return this.http.post<TaskComment>(`/api/tasks/${taskId}/comments`, { body });
  }

  update(commentId: number, body: string): Observable<TaskComment> {
    return this.http.put<TaskComment>(`/api/comments/${commentId}`, { body });
  }

  delete(commentId: number): Observable<void> {
    return this.http.delete<void>(`/api/comments/${commentId}`);
  }
}
