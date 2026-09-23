import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateTaskRequest,
  MoveTaskRequest,
  TaskCard,
  TaskDetails,
  UpdateTaskRequest,
} from './task.models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  get(id: number): Observable<TaskDetails> {
    return this.http.get<TaskDetails>(`/api/tasks/${id}`);
  }

  create(request: CreateTaskRequest): Observable<TaskDetails> {
    return this.http.post<TaskDetails>('/api/tasks', request);
  }

  update(id: number, request: UpdateTaskRequest): Observable<TaskDetails> {
    return this.http.put<TaskDetails>(`/api/tasks/${id}`, request);
  }

  move(id: number, request: MoveTaskRequest): Observable<TaskCard> {
    return this.http.post<TaskCard>(`/api/tasks/${id}/move`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/tasks/${id}`);
  }
}
