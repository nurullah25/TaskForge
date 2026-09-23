import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Label } from './label.models';

@Injectable({ providedIn: 'root' })
export class LabelService {
  private readonly http = inject(HttpClient);

  listForProject(projectId: number): Observable<Label[]> {
    return this.http.get<Label[]>(`/api/projects/${projectId}/labels`);
  }

  create(projectId: number, name: string, color: string): Observable<Label> {
    return this.http.post<Label>(`/api/projects/${projectId}/labels`, { name, color });
  }

  update(labelId: number, name: string, color: string): Observable<Label> {
    return this.http.put<Label>(`/api/labels/${labelId}`, { name, color });
  }

  delete(labelId: number): Observable<void> {
    return this.http.delete<void>(`/api/labels/${labelId}`);
  }

  setTaskLabels(taskId: number, labelIds: number[]): Observable<Label[]> {
    return this.http.put<Label[]>(`/api/tasks/${taskId}/labels`, { labelIds });
  }
}
