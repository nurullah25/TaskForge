import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TaskListItem } from '../tasks/task-search.service';

export interface CountByName {
  name: string;
  count: number;
}

export interface DashboardSummary {
  projectCount: number;
  openTaskCount: number;
  completedTaskCount: number;
  overdueTaskCount: number;
  myOpenTaskCount: number;
  openByCategory: CountByName[];
  openByPriority: CountByName[];
  myTasks: TaskListItem[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  get(organizationId: number): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`/api/dashboard?organizationId=${organizationId}`);
  }
}
