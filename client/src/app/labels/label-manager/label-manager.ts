import { Component, inject, input, numberAttribute, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { catchError, EMPTY, filter, switchMap } from 'rxjs';
import { getErrorMessage, ignoreHandledError } from '../../core/http/api-error';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { Label, LABEL_COLORS } from '../label.models';
import { LabelService } from '../label.service';
import { LabelChip } from '../label-chip/label-chip';

// Label list for the project settings page: add, recolour and delete.
@Component({
  selector: 'app-label-manager',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    LabelChip,
  ],
  templateUrl: './label-manager.html',
  styleUrl: './label-manager.scss',
})
export class LabelManager {
  private readonly api = inject(LabelService);
  private readonly dialog = inject(MatDialog);

  readonly projectId = input.required({ transform: numberAttribute });
  readonly canManage = input(false);

  protected readonly colors = LABEL_COLORS;
  protected readonly labels = signal<Label[]>([]);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(30)]],
    color: [LABEL_COLORS[0]],
  });

  constructor() {
    toObservable(this.projectId)
      .pipe(
        switchMap((id) => this.api.listForProject(id).pipe(catchError(() => EMPTY))),
        takeUntilDestroyed(),
      )
      .subscribe((labels) => this.labels.set(labels));
  }

  protected add(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, color } = this.form.getRawValue();
    this.errorMessage.set(null);

    this.api.create(this.projectId(), name, color).subscribe({
      next: (label) => {
        this.labels.update((list) => [...list, label].sort((a, b) => a.name.localeCompare(b.name)));
        this.form.reset({ name: '', color: LABEL_COLORS[0] });
      },
      error: (error) => this.errorMessage.set(getErrorMessage(error)),
    });
  }

  protected recolour(label: Label, color: string): void {
    this.api
      .update(label.id, label.name, color)
      .pipe(ignoreHandledError())
      .subscribe((updated) =>
        this.labels.update((list) => list.map((l) => (l.id === updated.id ? updated : l))),
      );
  }

  protected remove(label: Label): void {
    confirmAction(this.dialog, {
      title: `Delete the label "${label.name}"?`,
      message: 'It is removed from every task that uses it.',
      confirmLabel: 'Delete label',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(label.id)),
        ignoreHandledError(),
      )
      .subscribe(() => this.labels.update((list) => list.filter((l) => l.id !== label.id)));
  }
}
