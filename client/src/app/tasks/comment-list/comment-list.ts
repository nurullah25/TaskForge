import { Component, inject, input, numberAttribute, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { catchError, EMPTY, filter, switchMap } from 'rxjs';
import { ignoreHandledError } from '../../core/http/api-error';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { UserAvatar } from '../../shared/components/user-avatar/user-avatar';
import { RelativeTimePipe } from '../../shared/pipes/relative-time.pipe';
import { CommentService, TaskComment } from '../comment.service';

@Component({
  selector: 'app-comment-list',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    UserAvatar,
    RelativeTimePipe,
  ],
  templateUrl: './comment-list.html',
  styleUrl: './comment-list.scss',
})
export class CommentList {
  private readonly api = inject(CommentService);
  private readonly dialog = inject(MatDialog);

  readonly taskId = input.required({ transform: numberAttribute });
  readonly countChanged = output<number>();

  protected readonly comments = signal<TaskComment[]>([]);
  protected readonly total = signal(0);
  protected readonly loading = signal(true);
  protected readonly editingId = signal<number | null>(null);

  // A group rather than a lone control so the form element gets Angular's submit handling.
  protected readonly newCommentForm = new FormGroup({
    body: new FormControl('', { nonNullable: true, validators: Validators.required }),
  });
  protected readonly editedComment = new FormControl('', {
    nonNullable: true,
    validators: Validators.required,
  });

  private page = 1;

  constructor() {
    toObservable(this.taskId)
      .pipe(
        switchMap((id) => {
          this.page = 1;
          this.loading.set(true);
          return this.api.list(id, this.page).pipe(catchError(() => EMPTY));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.comments.set(result.items);
        this.total.set(result.totalCount);
        this.loading.set(false);
      });
  }

  protected loadMore(): void {
    this.page += 1;

    this.api
      .list(this.taskId(), this.page)
      .pipe(ignoreHandledError())
      .subscribe((result) => {
        this.comments.update((list) => [...list, ...result.items]);
        this.total.set(result.totalCount);
      });
  }

  protected add(): void {
    const body = this.newCommentForm.controls.body.value.trim();
    if (!body) {
      return;
    }

    this.api
      .add(this.taskId(), body)
      .pipe(ignoreHandledError())
      .subscribe((comment) => {
        this.comments.update((list) => [...list, comment]);
        this.newCommentForm.reset();
        this.changeTotal(1);
      });
  }

  protected startEditing(comment: TaskComment): void {
    this.editingId.set(comment.id);
    this.editedComment.setValue(comment.body);
  }

  protected saveEdit(comment: TaskComment): void {
    const body = this.editedComment.value.trim();
    if (!body) {
      return;
    }

    this.api
      .update(comment.id, body)
      .pipe(ignoreHandledError())
      .subscribe((updated) => {
        this.comments.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
        this.editingId.set(null);
      });
  }

  protected remove(comment: TaskComment): void {
    confirmAction(this.dialog, {
      title: 'Delete comment?',
      message: 'The comment is removed for everyone.',
      confirmLabel: 'Delete',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(comment.id)),
        ignoreHandledError(),
      )
      .subscribe(() => {
        this.comments.update((list) => list.filter((c) => c.id !== comment.id));
        this.changeTotal(-1);
      });
  }

  private changeTotal(delta: number): void {
    this.total.update((value) => value + delta);
    this.countChanged.emit(this.total());
  }
}
