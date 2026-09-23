import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/auth/auth.service';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { OrganizationCreateDialog } from '../../organizations/organization-create-dialog/organization-create-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { PageHeader } from '../../shared/components/page-header/page-header';

// Placeholder until the dashboard statistics are built.
@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, MatButtonModule, PageHeader, EmptyState],
  template: `
    <app-page-header [title]="greeting()" [subtitle]="(today | date: 'EEEE, MMMM d') ?? ''" />

    @if (workspace.organizationsLoaded() && workspace.organizations().length === 0) {
      <app-empty-state
        icon="rocket_launch"
        title="Welcome to TaskForge"
        message="Create an organization for your team, then add projects and invite colleagues."
      >
        <button mat-flat-button (click)="createOrganization()">Create organization</button>
      </app-empty-state>
    } @else {
      <app-empty-state
        icon="insights"
        title="Your overview will show up here"
        message="Projects, open tasks and deadlines will be summarised on this page."
      />
    }
  `,
})
export class DashboardPage {
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  protected readonly workspace = inject(WorkspaceService);

  protected readonly today = new Date();

  protected readonly greeting = computed(() => {
    const hour = this.today.getHours();
    const partOfDay = hour < 12 ? 'morning' : hour < 18 ? 'afternoon' : 'evening';
    const firstName = this.auth.currentUser()?.fullName.split(' ')[0] ?? '';
    return `Good ${partOfDay}, ${firstName}`;
  });

  protected createOrganization(): void {
    OrganizationCreateDialog.open(this.dialog).subscribe();
  }
}
