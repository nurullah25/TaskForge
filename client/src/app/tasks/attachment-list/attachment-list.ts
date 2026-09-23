import {
  Component,
  inject,
  input,
  numberAttribute,
  signal,
  viewChild,
  ElementRef,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { catchError, EMPTY, filter, finalize, switchMap } from 'rxjs';
import { getErrorMessage, ignoreHandledError } from '../../core/http/api-error';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';
import { Attachment, AttachmentService, formatFileSize } from '../attachment.service';

@Component({
  selector: 'app-attachment-list',
  imports: [MatButtonModule, MatIconModule, RelativeTimePipe],
  templateUrl: './attachment-list.html',
  styleUrl: './attachment-list.scss',
})
export class AttachmentList {
  private readonly api = inject(AttachmentService);
  private readonly dialog = inject(MatDialog);
  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  readonly taskId = input.required({ transform: numberAttribute });
  readonly canUpload = input(false);

  protected readonly attachments = signal<Attachment[]>([]);
  protected readonly uploading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly formatSize = formatFileSize;

  constructor() {
    toObservable(this.taskId)
      .pipe(
        switchMap((id) => this.api.list(id).pipe(catchError(() => EMPTY))),
        takeUntilDestroyed(),
      )
      .subscribe((attachments) => this.attachments.set(attachments));
  }

  protected chooseFile(): void {
    this.fileInput().nativeElement.click();
  }

  protected upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    this.uploading.set(true);
    this.errorMessage.set(null);

    this.api
      .upload(this.taskId(), file)
      .pipe(finalize(() => this.uploading.set(false)))
      .subscribe({
        next: (attachment) => this.attachments.update((list) => [attachment, ...list]),
        error: (error) => this.errorMessage.set(getErrorMessage(error)),
      });
  }

  // The request needs the access token, so the file is fetched and then saved
  // through a temporary link instead of pointing the browser at the URL.
  protected download(attachment: Attachment): void {
    this.api
      .download(attachment)
      .pipe(ignoreHandledError())
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = attachment.fileName;
        link.click();
        URL.revokeObjectURL(url);
      });
  }

  protected remove(attachment: Attachment): void {
    confirmAction(this.dialog, {
      title: `Delete ${attachment.fileName}?`,
      message: 'The file is removed from this task permanently.',
      confirmLabel: 'Delete file',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(attachment.id)),
        ignoreHandledError(),
      )
      .subscribe(() =>
        this.attachments.update((list) => list.filter((a) => a.id !== attachment.id)),
      );
  }
}
