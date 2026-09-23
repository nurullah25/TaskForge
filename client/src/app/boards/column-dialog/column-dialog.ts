import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Observable } from 'rxjs';
import { BoardColumn, COLUMN_CATEGORIES, ColumnCategory, SaveColumnRequest } from '../board.models';

@Component({
  selector: 'app-column-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ column ? 'Edit column' : 'New column' }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Column name</mat-label>
          <input matInput formControlName="name" placeholder="e.g. Code review" cdkFocusInitial />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Name is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Type</mat-label>
          <mat-select formControlName="category">
            @for (category of categories; track category.value) {
              <mat-option [value]="category.value">{{ category.label }}</mat-option>
            }
          </mat-select>
          <mat-hint>{{ selectedHint() }}</mat-hint>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">{{ column ? 'Save' : 'Add column' }}</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class ColumnDialog {
  private readonly dialogRef = inject(MatDialogRef<ColumnDialog, SaveColumnRequest>);
  protected readonly column = inject<BoardColumn | null>(MAT_DIALOG_DATA);
  protected readonly categories = COLUMN_CATEGORIES;

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [this.column?.name ?? '', [Validators.required, Validators.maxLength(50)]],
    category: [this.column?.category ?? ('ToDo' as ColumnCategory)],
  });

  static open(dialog: MatDialog, column: BoardColumn | null = null): Observable<SaveColumnRequest | undefined> {
    return dialog.open(ColumnDialog, { width: '420px', data: column }).afterClosed();
  }

  protected selectedHint(): string {
    const selected = this.form.controls.category.value;
    return this.categories.find((c) => c.value === selected)?.hint ?? '';
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue());
  }
}
