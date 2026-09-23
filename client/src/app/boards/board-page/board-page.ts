import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { Component, effect, inject, input, numberAttribute } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { WorkspaceService } from '../../core/workspace/workspace.service';
import { confirmAction } from '../../shared/components/confirm-dialog/confirm-dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { BoardColumn } from '../board.models';
import { BoardService } from '../board.service';
import { BoardStore } from '../board.store';
import { BoardColumnComponent } from '../board-column/board-column';
import { ColumnDialog } from '../column-dialog/column-dialog';
import { RenameDialog } from '../rename-dialog/rename-dialog';

@Component({
  selector: 'app-board-page',
  imports: [
    CdkDropList,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressBarModule,
    BoardColumnComponent,
    EmptyState,
  ],
  providers: [BoardStore],
  templateUrl: './board-page.html',
  styleUrl: './board-page.scss',
})
export class BoardPage {
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly api = inject(BoardService);
  private readonly workspace = inject(WorkspaceService);
  protected readonly store = inject(BoardStore);

  readonly boardId = input.required({ transform: numberAttribute });

  constructor() {
    effect(() => this.store.load(this.boardId()));
  }

  protected addColumn(): void {
    ColumnDialog.open(this.dialog)
      .pipe(filter(Boolean))
      .subscribe((request) => this.store.addColumn(request));
  }

  protected editColumn(column: BoardColumn): void {
    ColumnDialog.open(this.dialog, column)
      .pipe(filter(Boolean))
      .subscribe((request) => this.store.updateColumn(column.id, request));
  }

  protected deleteColumn(column: BoardColumn): void {
    confirmAction(this.dialog, {
      title: `Delete ${column.name}?`,
      message: 'The column must be empty. Tasks in it have to be moved first.',
      confirmLabel: 'Delete column',
      destructive: true,
    })
      .pipe(filter(Boolean))
      .subscribe(() => this.store.deleteColumn(column.id));
  }

  protected dropColumn(event: CdkDragDrop<BoardColumn[]>): void {
    if (event.previousIndex !== event.currentIndex) {
      this.store.moveColumn(event.previousIndex, event.currentIndex);
    }
  }

  protected renameBoard(): void {
    const board = this.store.board()!;

    RenameDialog.open(this.dialog, { title: 'Rename board', label: 'Board name', value: board.name })
      .pipe(filter(Boolean))
      .subscribe((name) => this.store.renameBoard(name));
  }

  protected createBoard(): void {
    const board = this.store.board()!;

    RenameDialog.open(this.dialog, { title: 'New board', label: 'Board name', value: '', confirmLabel: 'Create board' })
      .pipe(
        filter(Boolean),
        switchMap((name) => this.api.create(board.projectId, name)),
      )
      .subscribe((created) => this.router.navigate(['/projects', created.projectId, 'boards', created.id]));
  }

  protected deleteBoard(): void {
    const board = this.store.board()!;

    confirmAction(this.dialog, {
      title: `Delete ${board.name}?`,
      message: 'The board must have no tasks left, and a project needs at least one board.',
      confirmLabel: 'Delete board',
      destructive: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.api.delete(board.id)),
      )
      .subscribe(() => {
        this.workspace.reloadProjects();
        this.router.navigate(['/projects', board.projectId]);
      });
  }
}
