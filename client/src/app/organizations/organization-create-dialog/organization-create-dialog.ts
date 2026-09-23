import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Observable } from 'rxjs';
import { getErrorMessage } from '../../core/http/api-error';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { Organization } from '../organization.models';
import { OrganizationService } from '../organization.service';

@Component({
  selector: 'app-organization-create-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Create organization</h2>
    <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <mat-dialog-content>
        <p class="muted intro">An organization holds your team's projects. You can add people once it's created.</p>

        @if (errorMessage(); as message) {
          <div class="form-error" role="alert"><mat-icon>error</mat-icon>{{ message }}</div>
        }

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Organization name</mat-label>
          <input matInput formControlName="name" placeholder="e.g. Acme Inc." cdkFocusInitial />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Name is required</mat-error>
          } @else if (form.controls.name.hasError('maxlength')) {
            <mat-error>Keep it under 100 characters</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">Create</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .intro {
      margin: 0 0 16px;
    }
  `,
})
export class OrganizationCreateDialog {
  private readonly api = inject(OrganizationService);
  private readonly workspace = inject(WorkspaceService);
  private readonly dialogRef = inject(MatDialogRef<OrganizationCreateDialog, Organization>);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  static open(dialog: MatDialog): Observable<Organization | undefined> {
    return dialog.open(OrganizationCreateDialog, { width: '460px' }).afterClosed();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.api.create(this.form.getRawValue().name).subscribe({
      next: (organization) => {
        this.workspace.saveOrganization(organization);
        this.dialogRef.close(organization);
      },
      error: (error) => {
        this.saving.set(false);
        this.errorMessage.set(getErrorMessage(error));
      },
    });
  }
}
