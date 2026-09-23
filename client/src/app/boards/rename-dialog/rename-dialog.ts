import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Observable } from 'rxjs';

export interface RenameDialogData {
  title: string;
  label: string;
  value: string;
  confirmLabel?: string;
}

// Small dialog for the "just one name" cases, such as creating or renaming a board.
@Component({
  selector: 'app-rename-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>{{ data.label }}</mat-label>
          <input matInput formControlName="value" cdkFocusInitial />
          @if (form.controls.value.hasError('required')) {
            <mat-error>{{ data.label }} is required</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit">{{ data.confirmLabel ?? 'Save' }}</button>
      </mat-dialog-actions>
    </form>
  `,
})
export class RenameDialog {
  private readonly dialogRef = inject(MatDialogRef<RenameDialog, string>);
  protected readonly data = inject<RenameDialogData>(MAT_DIALOG_DATA);

  protected readonly form = inject(NonNullableFormBuilder).group({
    value: [this.data.value, [Validators.required, Validators.maxLength(100)]],
  });

  static open(dialog: MatDialog, data: RenameDialogData): Observable<string | undefined> {
    return dialog.open(RenameDialog, { width: '420px', data }).afterClosed();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.getRawValue().value.trim());
  }
}
