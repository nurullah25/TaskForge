import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Organization, OrganizationMember, OrganizationRole } from './organization.models';

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);

  list(): Observable<Organization[]> {
    return this.http.get<Organization[]>('/api/organizations');
  }

  get(id: number): Observable<Organization> {
    return this.http.get<Organization>(`/api/organizations/${id}`);
  }

  create(name: string): Observable<Organization> {
    return this.http.post<Organization>('/api/organizations', { name });
  }

  rename(id: number, name: string): Observable<Organization> {
    return this.http.put<Organization>(`/api/organizations/${id}`, { name });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`/api/organizations/${id}`);
  }

  members(id: number): Observable<OrganizationMember[]> {
    return this.http.get<OrganizationMember[]>(`/api/organizations/${id}/members`);
  }

  addMember(id: number, email: string, role: OrganizationRole): Observable<OrganizationMember> {
    return this.http.post<OrganizationMember>(`/api/organizations/${id}/members`, { email, role });
  }

  updateMember(id: number, userId: number, role: OrganizationRole): Observable<OrganizationMember> {
    return this.http.put<OrganizationMember>(`/api/organizations/${id}/members/${userId}`, { role });
  }

  removeMember(id: number, userId: number): Observable<void> {
    return this.http.delete<void>(`/api/organizations/${id}/members/${userId}`);
  }
}
