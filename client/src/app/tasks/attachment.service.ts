import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { TaskMember } from './task.models';

export interface Attachment {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: TaskMember;
  createdAt: string;
  canDelete: boolean;
}

@Injectable({ providedIn: 'root' })
export class AttachmentService {
  private readonly http = inject(HttpClient);

  list(taskId: number): Observable<Attachment[]> {
    return this.http.get<Attachment[]>(`/api/tasks/${taskId}/attachments`);
  }

  upload(taskId: number, file: File): Observable<Attachment> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<Attachment>(`/api/tasks/${taskId}/attachments`, form);
  }

  delete(attachmentId: number): Observable<void> {
    return this.http.delete<void>(`/api/attachments/${attachmentId}`);
  }

  // Downloads go through HttpClient so the access token is attached, then the blob
  // is handed to the browser as a normal file download.
  download(attachment: Attachment): Observable<Blob> {
    return this.http.get(`/api/attachments/${attachment.id}/download`, { responseType: 'blob' });
  }
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const kilobytes = bytes / 1024;
  return kilobytes < 1024 ? `${Math.round(kilobytes)} KB` : `${(kilobytes / 1024).toFixed(1)} MB`;
}
