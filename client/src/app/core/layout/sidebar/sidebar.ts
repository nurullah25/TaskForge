import { Component, computed, inject, output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { OrganizationCreateDialog } from '../../../organizations/organization-create-dialog/organization-create-dialog';
import { WorkspaceService } from '../../workspace/workspace.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  imports: [
    MatListModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    RouterLink,
    RouterLinkActive,
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  protected readonly workspace = inject(WorkspaceService);

  readonly linkClicked = output();

  protected readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'space_dashboard', route: '/dashboard' },
    { label: 'Projects', icon: 'folder', route: '/projects' },
  ];

  protected readonly activeProjects = computed(() =>
    this.workspace.projects().filter((p) => p.status !== 'Archived'),
  );

  protected selectOrganization(id: number): void {
    this.workspace.selectOrganization(id);
    this.router.navigateByUrl('/projects');
    this.linkClicked.emit();
  }

  protected createOrganization(): void {
    OrganizationCreateDialog.open(this.dialog).subscribe((organization) => {
      if (organization) {
        this.router.navigateByUrl('/projects');
        this.linkClicked.emit();
      }
    });
  }
}
