import { Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { OrganizationCreateDialog } from '../../organizations/organization-create-dialog/organization-create-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { PageHeader } from '../../shared/components/page-header/page-header';
import { ProjectCard } from '../project-card/project-card';
import { ProjectCreateDialog } from '../project-create-dialog/project-create-dialog';

type StatusFilter = 'current' | 'all' | 'archived';

@Component({
  selector: 'app-project-list-page',
  imports: [
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatProgressBarModule,
    PageHeader,
    EmptyState,
    ProjectCard,
  ],
  templateUrl: './project-list-page.html',
  styles: `
    .toolbar {
      margin-bottom: 20px;
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 16px;
    }
  `,
})
export class ProjectListPage {
  private readonly dialog = inject(MatDialog);
  protected readonly workspace = inject(WorkspaceService);

  protected readonly statusFilter = signal<StatusFilter>('current');

  protected readonly visibleProjects = computed(() => {
    const projects = this.workspace.projects();
    switch (this.statusFilter()) {
      case 'current':
        return projects.filter((p) => p.status !== 'Archived');
      case 'archived':
        return projects.filter((p) => p.status === 'Archived');
      default:
        return projects;
    }
  });

  protected createProject(): void {
    const organization = this.workspace.currentOrganization();
    if (organization) {
      ProjectCreateDialog.open(this.dialog, organization.id);
    }
  }

  protected createOrganization(): void {
    OrganizationCreateDialog.open(this.dialog).subscribe();
  }
}
