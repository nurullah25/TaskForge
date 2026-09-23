import { Component, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  EMPTY,
  map,
  startWith,
  switchMap,
} from 'rxjs';
import { ColumnCategory } from '../../boards/board.models';
import { Label } from '../../labels/label.models';
import { LabelService } from '../../labels/label.service';
import { ProjectService } from '../../projects/project.service';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { TASK_PRIORITIES, TaskMember, TaskPriority } from '../task.models';
import { TaskFilters, TaskListItem, TaskSearchService, TaskSort } from '../task-search.service';
import { TaskListRow } from '../task-list-row/task-list-row';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-task-search-page',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    PageHeader,
    EmptyState,
    TaskListRow,
  ],
  templateUrl: './task-search-page.html',
  styleUrl: './task-search-page.scss',
})
export class TaskSearchPage {
  private readonly api = inject(TaskSearchService);
  private readonly projects = inject(ProjectService);
  private readonly labelApi = inject(LabelService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly projectId = input.required({ transform: numberAttribute });

  protected readonly priorities = TASK_PRIORITIES;
  protected readonly categories: { value: ColumnCategory; label: string }[] = [
    { value: 'ToDo', label: 'To do' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'Done', label: 'Done' },
  ];

  protected readonly members = signal<TaskMember[]>([]);
  protected readonly labels = signal<Label[]>([]);
  protected readonly results = signal<TaskListItem[]>([]);
  protected readonly total = signal(0);
  protected readonly loading = signal(true);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = PAGE_SIZE;

  // The filters live in the URL, so a filtered list can be bookmarked or shared.
  private readonly params = this.route.snapshot.queryParamMap;
  protected readonly form = new FormGroup({
    text: new FormControl(this.params.get('text') ?? ''),
    assigneeId: new FormControl(numberOrNull(this.params.get('assigneeId'))),
    priority: new FormControl(this.params.get('priority') as TaskPriority | null),
    category: new FormControl(this.params.get('category') as ColumnCategory | null),
    labelId: new FormControl(numberOrNull(this.params.get('labelId'))),
    sort: new FormControl((this.params.get('sort') as TaskSort) ?? 'Newest'),
  });

  protected readonly activeFilterCount = signal(0);
  protected readonly projectKey = signal('');

  constructor() {
    toObservable(this.projectId)
      .pipe(
        switchMap((id) =>
          combineLatest([
            this.projects.members(id),
            this.labelApi.listForProject(id),
            this.projects.get(id),
          ]).pipe(catchError(() => EMPTY)),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(([members, labels, project]) => {
        this.members.set(
          members.map((m) => ({ id: m.userId, fullName: m.fullName, email: m.email })),
        );
        this.labels.set(labels);
        this.projectKey.set(project.key);
      });

    // Typing waits a moment and repeated values are ignored, so one keystroke per
    // word doesn't turn into one request per keystroke.
    const filters$ = this.form.valueChanges.pipe(
      startWith(this.form.value),
      debounceTime(300),
      map(() => this.currentFilters()),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
    );

    combineLatest([toObservable(this.projectId), filters$, toObservable(this.pageIndex)])
      .pipe(
        switchMap(([projectId, filters, pageIndex]) => {
          this.loading.set(true);
          this.updateUrl(filters);
          // switchMap drops the response of a filter the user has already changed.
          return this.api
            .search(projectId, { ...filters, page: pageIndex + 1, pageSize: PAGE_SIZE })
            .pipe(catchError(() => EMPTY));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.results.set(result.items);
        this.total.set(result.totalCount);
        this.loading.set(false);
      });
  }

  protected changePage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
  }

  protected clearFilters(): void {
    this.form.reset({
      text: '',
      assigneeId: null,
      priority: null,
      category: null,
      labelId: null,
      sort: 'Newest',
    });
    this.pageIndex.set(0);
  }

  protected openTask(task: TaskListItem): void {
    this.router.navigate(['/boards', task.boardId], { queryParams: { task: task.id } });
  }

  private currentFilters(): TaskFilters {
    const value = this.form.getRawValue();
    const filters: TaskFilters = {
      text: value.text?.trim() || null,
      assigneeId: value.assigneeId,
      priority: value.priority,
      category: value.category,
      labelId: value.labelId,
      sort: value.sort ?? 'Newest',
    };

    this.activeFilterCount.set(
      [
        filters.text,
        filters.assigneeId,
        filters.priority,
        filters.category,
        filters.labelId,
      ].filter(Boolean).length,
    );

    return filters;
  }

  private updateUrl(filters: TaskFilters): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        text: filters.text || null,
        assigneeId: filters.assigneeId ?? null,
        priority: filters.priority ?? null,
        category: filters.category ?? null,
        labelId: filters.labelId ?? null,
        sort: filters.sort === 'Newest' ? null : filters.sort,
      },
      replaceUrl: true,
    });
  }
}

function numberOrNull(value: string | null): number | null {
  return value ? Number(value) : null;
}
