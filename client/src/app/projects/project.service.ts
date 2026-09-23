import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateProjectRequest,
  Project,
  ProjectDetails,
  ProjectMember,
  ProjectRole,
  UpdateProjectRequest,
} from './project.models';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);

  list(organizationId: number): Observable<Project[]> {
    return this.http.get<Project[]>(`/api/organizations/${organizationId}/projects`);
  }

  get(id: number): Observable<ProjectDetails> {
    return this.http.get<ProjectDetails>(`/api/projects/${id}`);
  }

  create(organizationId: number, request: CreateProjectRequest): Observable<ProjectDetails> {
    return this.http.post<ProjectDetails>(`/api/organizations/${organizationId}/projects`, request);
  }

  update(id: number, request: UpdateProjectRequest): Observable<ProjectDetails> {
    return this.http.put<ProjectDetails>(`/api/projects/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${id}`);
  }

  members(id: number): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`/api/projects/${id}/members`);
  }

  addMember(id: number, userId: number, role: ProjectRole): Observable<ProjectMember> {
    return this.http.post<ProjectMember>(`/api/projects/${id}/members`, { userId, role });
  }

  updateMember(id: number, userId: number, role: ProjectRole): Observable<ProjectMember> {
    return this.http.put<ProjectMember>(`/api/projects/${id}/members/${userId}`, { role });
  }

  removeMember(id: number, userId: number): Observable<void> {
    return this.http.delete<void>(`/api/projects/${id}/members/${userId}`);
  }
}
