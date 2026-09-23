import { DatePipe } from '@angular/common';
import { Component, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { catchError, EMPTY, switchMap, tap } from 'rxjs';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ActivityEntry, ActivityService } from '../../tasks/activity.service';
import { ActivityList } from '../../tasks/activity-list/activity-list';
import { ProjectDetails } from '../project.models';
import { ProjectService } from '../project.service';
import { ProjectStatusChip } from '../project-status-chip/project-status-chip';

@Component({
  selector: 'app-project-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    RouterLink,
    EmptyState,
    ProjectStatusChip,
    ActivityList,
  ],
  templateUrl: './project-page.html',
  styleUrl: './project-page.scss',
})
export class ProjectPage {
  private readonly api = inject(ProjectService);
  private readonly activityApi = inject(ActivityService);

  readonly projectId = input.required({ transform: numberAttribute });

  protected readonly project = signal<ProjectDetails | null>(null);
  protected readonly recentActivity = signal<ActivityEntry[]>([]);
  protected readonly notFound = signal(false);

  constructor() {
    toObservable(this.projectId)
      .pipe(
        tap(() => this.notFound.set(false)),
        switchMap((id) =>
          this.api.get(id).pipe(
            catchError(() => {
              this.notFound.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((project) => this.project.set(project));

    toObservable(this.projectId)
      .pipe(
        switchMap((id) => this.activityApi.forProject(id, 1, 10).pipe(catchError(() => EMPTY))),
        takeUntilDestroyed(),
      )
      .subscribe((result) => this.recentActivity.set(result.items));
  }
}
