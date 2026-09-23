import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Project } from '../project.models';
import { ProjectStatusChip } from '../project-status-chip/project-status-chip';

@Component({
  selector: 'app-project-card',
  imports: [MatCardModule, MatIconModule, RouterLink, ProjectStatusChip],
  template: `
    @let p = project();
    <a class="card-link" [routerLink]="['/projects', p.id]">
      <mat-card appearance="outlined" class="card">
        <div class="top">
          <span class="key-badge">{{ p.key }}</span>
          <app-project-status-chip [status]="p.status" />
        </div>

        <h3>{{ p.name }}</h3>
        <p class="description muted">{{ p.description || 'No description' }}</p>

        <div class="stats muted">
          <span><mat-icon>group</mat-icon>{{ p.memberCount }}</span>
          <span><mat-icon>radio_button_unchecked</mat-icon>{{ p.openTaskCount }} open</span>
          <span class="role">{{ p.myRole }}</span>
        </div>
      </mat-card>
    </a>
  `,
  styles: `
    .card-link {
      display: block;
      height: 100%;
      color: inherit;
      text-decoration: none;
    }

    .card {
      height: 100%;
      padding: 20px;
      box-sizing: border-box;
      background: var(--mat-sys-surface);
      transition:
        border-color 0.15s,
        box-shadow 0.15s;

      &:hover {
        border-color: var(--mat-sys-primary);
        box-shadow: 0 2px 8px rgb(0 0 0 / 6%);
      }
    }

    .top {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    h3 {
      margin: 14px 0 6px;
      font-size: 16px;
      font-weight: 600;
    }

    .description {
      display: -webkit-box;
      min-height: 40px;
      margin: 0 0 16px;
      overflow: hidden;
      font-size: 14px;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
    }

    .stats {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-top: auto;
      font-size: 13px;

      span {
        display: inline-flex;
        align-items: center;
        gap: 4px;
      }

      mat-icon {
        font-size: 16px;
        width: 16px;
        height: 16px;
      }
    }

    .role {
      margin-left: auto;
    }
  `,
})
export class ProjectCard {
  readonly project = input.required<Project>();
}
