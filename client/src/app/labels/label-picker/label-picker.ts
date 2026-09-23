import { Component, computed, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { Label } from '../label.models';
import { LabelChip } from '../label-chip/label-chip';

// Shows the labels on a task and lets an editor tick the ones that apply.
@Component({
  selector: 'app-label-picker',
  imports: [MatMenuModule, MatButtonModule, MatIconModule, MatCheckboxModule, LabelChip],
  template: `
    <div class="labels">
      @for (label of selected(); track label.id) {
        <app-label-chip [label]="label" />
      } @empty {
        <span class="muted no-labels">No labels</span>
      }

      @if (canEdit() && available().length) {
        <button
          mat-icon-button
          class="edit"
          [matMenuTriggerFor]="menu"
          aria-label="Choose labels"
          (menuClosed)="commit()"
        >
          <mat-icon>label</mat-icon>
        </button>

        <mat-menu #menu="matMenu">
          <div class="menu" (click)="$event.stopPropagation()">
            @for (label of available(); track label.id) {
              <mat-checkbox [checked]="isSelected(label)" (change)="toggle(label)">
                <app-label-chip [label]="label" />
              </mat-checkbox>
            }
          </div>
        </mat-menu>
      }
    </div>
  `,
  styles: `
    .labels {
      display: flex;
      align-items: center;
      gap: 6px;
      flex-wrap: wrap;
    }

    .no-labels {
      font-size: 13px;
    }

    .menu {
      display: flex;
      flex-direction: column;
      padding: 4px 12px;
    }
  `,
})
export class LabelPicker {
  readonly available = input.required<Label[]>();
  readonly canEdit = input(false);
  readonly labels = input.required<Label[]>();

  readonly changed = output<number[]>();

  private readonly pending = signal<number[] | null>(null);

  protected readonly selectedIds = computed(() => this.pending() ?? this.labels().map((l) => l.id));
  protected readonly selected = computed(() => {
    const ids = this.selectedIds();
    return this.available().filter((label) => ids.includes(label.id));
  });

  protected isSelected(label: Label): boolean {
    return this.selectedIds().includes(label.id);
  }

  protected toggle(label: Label): void {
    const ids = this.selectedIds();
    this.pending.set(
      ids.includes(label.id) ? ids.filter((id) => id !== label.id) : [...ids, label.id],
    );
  }

  // Saved once when the menu closes, instead of one request per tick.
  protected commit(): void {
    const pending = this.pending();
    this.pending.set(null);

    if (
      pending &&
      !sameIds(
        pending,
        this.labels().map((l) => l.id),
      )
    ) {
      this.changed.emit(pending);
    }
  }
}

function sameIds(a: number[], b: number[]): boolean {
  return a.length === b.length && [...a].sort().every((id, index) => id === [...b].sort()[index]);
}
