import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';
import { catchError, EMPTY, of, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { OrganizationCreateDialog } from '../../organizations/organization-create-dialog/organization-create-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { TaskListItem } from '../../tasks/task-search.service';
import { TaskListRow } from '../../tasks/task-list-row/task-list-row';
import { DashboardService, DashboardSummary } from '../dashboard.service';
import { BarChart } from '../bar-chart/bar-chart';
import { ChartSlice, DonutChart } from '../donut-chart/donut-chart';

const CATEGORY_COLORS: Record<string, string> = {
  ToDo: '#94a3b8',
  InProgress: '#2563eb',
  Done: '#16a34a',
};

const PRIORITY_COLORS: Record<string, string> = {
  Urgent: '#dc2626',
  High: '#ea580c',
  Medium: '#64748b',
  Low: '#0891b2',
};

const CATEGORY_LABELS: Record<string, string> = {
  ToDo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
};

@Component({
  selector: 'app-dashboard-page',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    PageHeader,
    EmptyState,
    TaskListRow,
    DonutChart,
    BarChart,
  ],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  private readonly auth = inject(AuthService);
  private readonly api = inject(DashboardService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  protected readonly workspace = inject(WorkspaceService);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly today = new Date();

  protected readonly greeting = computed(() => {
    const hour = this.today.getHours();
    const partOfDay = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    const firstName = this.auth.currentUser()?.fullName.split(' ')[0] ?? '';
    return `Good ${partOfDay}, ${firstName}`;
  });

  protected readonly byCategory = computed<ChartSlice[]>(() =>
    (this.summary()?.openByCategory ?? []).map((entry) => ({
      name: CATEGORY_LABELS[entry.name] ?? entry.name,
      count: entry.count,
      color: CATEGORY_COLORS[entry.name] ?? '#94a3b8',
    })),
  );

  protected readonly byPriority = computed<ChartSlice[]>(() =>
    (this.summary()?.openByPriority ?? []).map((entry) => ({
      name: entry.name,
      count: entry.count,
      color: PRIORITY_COLORS[entry.name] ?? '#64748b',
    })),
  );

  constructor() {
    // Reloads whenever the selected organization changes.
    toObservable(computed(() => this.workspace.currentOrganization()?.id ?? null))
      .pipe(
        switchMap((organizationId) =>
          organizationId === null
            ? of(null)
            : this.api.get(organizationId).pipe(catchError(() => EMPTY)),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((summary) => this.summary.set(summary));
  }

  protected openTask(task: TaskListItem): void {
    this.router.navigate(['/boards', task.boardId], { queryParams: { task: task.id } });
  }

  protected createOrganization(): void {
    OrganizationCreateDialog.open(this.dialog).subscribe();
  }
}
