import { Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { catchError, EMPTY, filter, forkJoin, of, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getErrorMessage } from '../../core/http/api-error';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { OrganizationMember } from '../../organizations/organization.models';
import { OrganizationService } from '../../organizations/organization.service';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { MemberList, MemberRow } from '../../shared/components/member-list/member-list';
import { PageHeader } from '../../shared/components/page-header/page-header';
import {
  PROJECT_ROLES,
  PROJECT_STATUSES,
  ProjectDetails,
  ProjectMember,
  ProjectRole,
  ProjectStatus,
} from '../project.models';
import { ProjectService } from '../project.service';

@Component({
  selector: 'app-project-settings-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    PageHeader,
    MemberList,
    EmptyState,
  ],
  templateUrl: './project-settings-page.html',
})
export class ProjectSettingsPage {
  private readonly api = inject(ProjectService);
  private readonly organizationApi = inject(OrganizationService);
  private readonly workspace = inject(WorkspaceService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly projectId = input.required({ transform: numberAttribute });

  protected readonly statuses = PROJECT_STATUSES;
  protected readonly roles = PROJECT_ROLES;

  protected readonly project = signal<ProjectDetails | null>(null);
  protected readonly members = signal<ProjectMember[]>([]);
  private readonly organizationMembers = signal<OrganizationMember[]>([]);
  protected readonly notFound = signal(false);
  protected readonly addError = signal<string | null>(null);

  protected readonly currentUserId = computed(() => this.auth.currentUser()?.id);
  protected readonly isManager = computed(() => this.project()?.myRole === 'Manager');
  protected readonly canEditMember = computed(() => (member: MemberRow) =>
    this.isManager() && member.userId !== this.currentUserId(),
  );

  // Organization members who aren't on the project yet.
  protected readonly candidates = computed(() => {
    const memberIds = new Set(this.members().map((m) => m.userId));
    return this.organizationMembers().filter((m) => !memberIds.has(m.userId));
  });

  protected readonly generalForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(2000)],
    status: ['Active' as ProjectStatus],
  });

  protected readonly addForm = this.formBuilder.group({
    userId: [0, Validators.min(1)],
    role: ['Contributor' as ProjectRole],
  });

  constructor() {
    toObservable(this.projectId)
      .pipe(
        switchMap((id) =>
          // The project is needed first because it tells us which organization to load members from.
          this.api.get(id).pipe(
            switchMap((project) =>
              forkJoin({
                project: of(project),
                members: this.api.members(id),
                organizationMembers: this.organizationApi.members(project.organizationId),
              }),
            ),
            catchError(() => {
              this.notFound.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(({ project, members, organizationMembers }) => {
        this.notFound.set(false);
        this.setProject(project);
        this.members.set(members);
        this.organizationMembers.set(organizationMembers);
      });
  }

  protected save(): void {
    const project = this.project();
    if (!project || this.generalForm.invalid) {
      this.generalForm.markAllAsTouched();
      return;
    }

    const { name, description, status } = this.generalForm.getRawValue();
    this.api.update(project.id, { name, description: description || null, status }).subscribe((updated) => {
      this.setProject(updated);
      this.workspace.reloadProjects();
      this.snackBar.open('Project saved', undefined, { duration: 3000 });
    });
  }

  protected addMember(): void {
    const project = this.project();
    if (!project || this.addForm.invalid) {
      return;
    }

    const { userId, role } = this.addForm.getRawValue();
    this.addError.set(null);

    this.api.addMember(project.id, userId, role).subscribe({
      next: (member) => {
        this.members.update((list) =>
          [...list, member].sort((a, b) => a.fullName.localeCompare(b.fullName)),
        );
        this.addForm.reset({ userId: 0, role: 'Contributor' });
      },
      error: (error) => this.addError.set(getErrorMessage(error)),
    });
  }

  protected changeRole(member: MemberRow, role: string): void {
    const project = this.project()!;

    this.api.updateMember(project.id, member.userId, role as ProjectRole).subscribe({
      next: (updated) =>
        this.members.update((list) => list.map((m) => (m.userId === updated.userId ? updated : m))),
      error: () => this.members.update((list) => list.map((m) => ({ ...m }))),
    });
  }

  protected removeMember(member: MemberRow): void {
    const project = this.project()!;
    const leaving = member.userId === this.currentUserId();

    confirmAction(this.dialog, {
      title: leaving ? 'Leave project?' : `Remove ${member.fullName}?`,
      message: leaving
        ? `You'll no longer see ${project.name} unless someone adds you back.`
        : `${member.fullName} will lose access to ${project.name}. Their open tasks become unassigned.`,
      confirmLabel: leaving ? 'Leave' : 'Remove',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.removeMember(project.id, member.userId)),
      )
      .subscribe(() => {
        this.members.update((list) => list.filter((m) => m.userId !== member.userId));
        if (leaving) {
          this.workspace.reloadProjects();
          this.router.navigateByUrl('/projects');
        }
      });
  }

  protected deleteProject(): void {
    const project = this.project()!;

    confirmAction(this.dialog, {
      title: `Delete ${project.name}?`,
      message: 'All boards, tasks, comments and history in this project are deleted permanently. This cannot be undone.',
      confirmLabel: 'Delete project',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(project.id)),
      )
      .subscribe(() => {
        this.workspace.reloadProjects();
        this.router.navigateByUrl('/projects');
        this.snackBar.open(`${project.name} was deleted`, undefined, { duration: 3000 });
      });
  }

  private setProject(project: ProjectDetails): void {
    this.project.set(project);
    this.generalForm.reset({
      name: project.name,
      description: project.description ?? '',
      status: project.status,
    });

    if (project.myRole === 'Manager') {
      this.generalForm.enable();
    } else {
      this.generalForm.disable();
    }
  }
}
