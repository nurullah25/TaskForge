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
import { Router } from '@angular/router';
import { catchError, EMPTY, filter, forkJoin, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { getErrorMessage } from '../../core/http/api-error';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { MemberList, MemberRow } from '../../shared/components/member-list/member-list';
import { PageHeader } from '../../shared/components/page-header/page-header';
import {
  canManageOrganization,
  Organization,
  ORGANIZATION_ROLES,
  OrganizationMember,
  OrganizationRole,
} from '../organization.models';
import { OrganizationService } from '../organization.service';

@Component({
  selector: 'app-organization-settings-page',
  imports: [
    ReactiveFormsModule,
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
  templateUrl: './organization-settings-page.html',
})
export class OrganizationSettingsPage {
  private readonly api = inject(OrganizationService);
  private readonly workspace = inject(WorkspaceService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  readonly organizationId = input.required({ transform: numberAttribute });

  protected readonly organization = signal<Organization | null>(null);
  protected readonly members = signal<OrganizationMember[]>([]);
  protected readonly notFound = signal(false);
  protected readonly addError = signal<string | null>(null);

  protected readonly currentUserId = computed(() => this.auth.currentUser()?.id);
  protected readonly canManage = computed(() => canManageOrganization(this.organization()?.myRole));
  protected readonly isOwner = computed(() => this.organization()?.myRole === 'Owner');

  // Mirrors the API rules: only owners hand out or take away the owner role.
  protected readonly assignableRoles = computed<OrganizationRole[]>(() =>
    this.isOwner() ? ORGANIZATION_ROLES : ['Admin', 'Member'],
  );
  protected readonly canEditMember = computed(() => (member: MemberRow) =>
    this.canManage() &&
    member.userId !== this.currentUserId() &&
    (this.isOwner() || member.role !== 'Owner'),
  );

  protected readonly nameForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  protected readonly addForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['Member' as OrganizationRole],
  });

  constructor() {
    toObservable(this.organizationId)
      .pipe(
        switchMap((id) =>
          forkJoin([this.api.get(id), this.api.members(id)]).pipe(
            catchError(() => {
              this.notFound.set(true);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(([organization, members]) => {
        this.notFound.set(false);
        this.organization.set(organization);
        this.members.set(members);
        this.nameForm.reset({ name: organization.name });
        if (this.canManage()) {
          this.nameForm.enable();
        } else {
          this.nameForm.disable();
        }
      });
  }

  protected rename(): void {
    const organization = this.organization();
    if (!organization || this.nameForm.invalid) {
      return;
    }

    this.api.rename(organization.id, this.nameForm.getRawValue().name).subscribe((updated) => {
      this.organization.set(updated);
      this.workspace.saveOrganization(updated);
      this.nameForm.markAsPristine();
      this.snackBar.open('Organization renamed', undefined, { duration: 3000 });
    });
  }

  protected addMember(): void {
    const organization = this.organization();
    if (!organization || this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const { email, role } = this.addForm.getRawValue();
    this.addError.set(null);

    this.api.addMember(organization.id, email, role).subscribe({
      next: (member) => {
        this.members.update((list) =>
          [...list, member].sort((a, b) => a.fullName.localeCompare(b.fullName)),
        );
        this.addForm.reset({ email: '', role: 'Member' });
      },
      error: (error) => this.addError.set(getErrorMessage(error)),
    });
  }

  protected changeRole(member: MemberRow, role: string): void {
    const organization = this.organization()!;

    this.api.updateMember(organization.id, member.userId, role as OrganizationRole).subscribe({
      next: (updated) =>
        this.members.update((list) => list.map((m) => (m.userId === updated.userId ? updated : m))),
      // Fresh objects re-render the rows, which puts the select back to the saved role.
      // The error message itself is shown by the error interceptor.
      error: () => this.members.update((list) => list.map((m) => ({ ...m }))),
    });
  }

  protected removeMember(member: MemberRow): void {
    const organization = this.organization()!;
    const leaving = member.userId === this.currentUserId();

    confirmAction(this.dialog, {
      title: leaving ? 'Leave organization?' : `Remove ${member.fullName}?`,
      message: leaving
        ? `You'll lose access to ${organization.name} and its projects.`
        : `${member.fullName} will lose access to ${organization.name} and its projects. Their open tasks become unassigned.`,
      confirmLabel: leaving ? 'Leave' : 'Remove',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.removeMember(organization.id, member.userId)),
      )
      .subscribe(() => {
        if (leaving) {
          this.workspace.removeOrganization(organization.id);
          this.router.navigateByUrl('/dashboard');
        } else {
          this.members.update((list) => list.filter((m) => m.userId !== member.userId));
        }
      });
  }

  protected deleteOrganization(): void {
    const organization = this.organization()!;

    confirmAction(this.dialog, {
      title: `Delete ${organization.name}?`,
      message: 'This permanently deletes the organization. It must not have any projects left.',
      confirmLabel: 'Delete organization',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(organization.id)),
      )
      .subscribe(() => {
        this.workspace.removeOrganization(organization.id);
        this.router.navigateByUrl('/dashboard');
      });
  }
}
