import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  imports: [MatIconModule],
  template: `
    <div class="empty-state">
      <mat-icon>{{ icon() }}</mat-icon>
      <h2>{{ title() }}</h2>
      @if (message()) {
        <p class="muted">{{ message() }}</p>
      }
      <div class="actions">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 48px 24px;
      border: 1px dashed var(--tf-border);
      border-radius: 12px;
      background: var(--mat-sys-surface);
      text-align: center;
    }

    mat-icon {
      font-size: 40px;
      width: 40px;
      height: 40px;
      color: var(--mat-sys-primary);
    }

    h2 {
      margin: 12px 0 4px;
      font-size: 17px;
      font-weight: 600;
    }

    p {
      max-width: 420px;
      margin: 0;
    }

    .actions:not(:empty) {
      margin-top: 20px;
    }
  `,
})
export class EmptyState {
  readonly icon = input('inbox');
  readonly title = input.required<string>();
  readonly message = input<string>();
}
