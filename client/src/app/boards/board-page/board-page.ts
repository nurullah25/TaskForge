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
import { TaskCard } from '../../tasks/task.models';
import { TaskCreateDialog } from '../../tasks/task-create-dialog/task-create-dialog';
import { TaskDetailPanel } from '../../tasks/task-detail-panel/task-detail-panel';
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

  // ?task=12 in the URL opens that task, so a card can be linked to directly.
  readonly task = input<string>();

  private openTaskId: number | null = null;

  constructor() {
    effect(() => this.store.load(this.boardId()));

    effect(() => {
      const taskId = Number(this.task());
      if (taskId > 0 && taskId !== this.openTaskId && this.store.board()) {
        this.openTaskPanel(taskId);
      }
    });
  }

  protected openTask(card: TaskCard): void {
    this.router.navigate([], { queryParams: { task: card.id }, queryParamsHandling: 'merge' });
  }

  protected createTask(column: BoardColumn): void {
    TaskCreateDialog.open(this.dialog, {
      columnId: column.id,
      columnName: column.name,
      members: this.store.members(),
    })
      .pipe(filter(Boolean))
      .subscribe((task) => this.store.addTask(task));
  }

  protected dropTask(event: CdkDragDrop<BoardColumn>): void {
    const card = event.item.data as TaskCard;
    const sameSpot =
      event.previousContainer === event.container && event.previousIndex === event.currentIndex;

    if (!sameSpot) {
      this.store.moveTask(card.id, event.container.data.id, event.currentIndex);
    }
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

    RenameDialog.open(this.dialog, {
      title: 'Rename board',
      label: 'Board name',
      value: board.name,
    })
      .pipe(filter(Boolean))
      .subscribe((name) => this.store.renameBoard(name));
  }

  protected createBoard(): void {
    const board = this.store.board()!;

    RenameDialog.open(this.dialog, {
      title: 'New board',
      label: 'Board name',
      value: '',
      confirmLabel: 'Create board',
    })
      .pipe(
        filter(Boolean),
        switchMap((name) => this.api.create(board.projectId, name)),
      )
      .subscribe((created) =>
        this.router.navigate(['/projects', created.projectId, 'boards', created.id]),
      );
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

  private openTaskPanel(taskId: number): void {
    this.openTaskId = taskId;

    TaskDetailPanel.open(this.dialog, {
      taskId,
      members: this.store.members(),
      labels: this.store.labels(),
    }).subscribe((result) => {
      this.openTaskId = null;
      if (result && 'changed' in result) {
        this.store.applyTaskChanges(result.changed, result.commentCount);
      } else if (result && 'deleted' in result) {
        this.store.removeTask(result.deleted);
      }

      this.router.navigate([], { queryParams: { task: null }, queryParamsHandling: 'merge' });
    });
  }
}
