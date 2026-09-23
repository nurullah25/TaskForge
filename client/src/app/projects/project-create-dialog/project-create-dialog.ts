import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { getErrorMessage } from '../../core/http/api-error';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { ProjectService } from '../project.service';

@Component({
  selector: 'app-project-create-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './project-create-dialog.html',
  styles: `
    .key-field {
      max-width: 180px;
    }

    input.uppercase {
      text-transform: uppercase;
    }
  `,
})
export class ProjectCreateDialog {
  private readonly api = inject(ProjectService);
  private readonly workspace = inject(WorkspaceService);
  private readonly router = inject(Router);
  private readonly dialogRef = inject(MatDialogRef<ProjectCreateDialog>);
  private readonly organizationId = inject<number>(MAT_DIALOG_DATA);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    key: ['', [Validators.required, Validators.pattern(/^[A-Za-z][A-Za-z0-9]{1,9}$/)]],
    description: ['', Validators.maxLength(2000)],
  });
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  static open(dialog: MatDialog, organizationId: number): void {
    dialog.open(ProjectCreateDialog, { width: '520px', data: organizationId });
  }

  constructor() {
    // Suggest a key from the name until the user types their own.
    this.form.controls.name.valueChanges.pipe(takeUntilDestroyed()).subscribe((name) => {
      if (!this.form.controls.key.dirty) {
        this.form.controls.key.setValue(suggestKey(name));
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, key, description } = this.form.getRawValue();
    this.saving.set(true);
    this.errorMessage.set(null);

    this.api
      .create(this.organizationId, {
        name,
        key: key.toUpperCase(),
        description: description || null,
      })
      .subscribe({
        next: (project) => {
          this.workspace.reloadProjects();
          this.dialogRef.close();
          this.router.navigate(['/projects', project.id]);
        },
        error: (error) => {
          this.saving.set(false);
          this.errorMessage.set(getErrorMessage(error));
        },
      });
  }
}

// "Customer Portal" -> "CP", "Website" -> "WEB"
export function suggestKey(name: string): string {
  const words = name.toUpperCase().match(/[A-Z0-9]+/g) ?? [];
  const key = words.length > 1 ? words.map((w) => w[0]).join('') : (words[0] ?? '').slice(0, 3);
  return key.replace(/^[0-9]+/, '').slice(0, 10);
}
