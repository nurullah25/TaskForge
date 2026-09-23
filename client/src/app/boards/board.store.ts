import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, forkJoin, of, switchMap } from 'rxjs';
import { ignoreHandledError } from '../core/http/api-error';
import { BoardEvent, RealtimeService } from '../core/realtime/realtime.service';
import { Label } from '../labels/label.models';
import { LabelService } from '../labels/label.service';
import { ProjectService } from '../projects/project.service';
import { TaskCard, TaskDetails, TaskMember } from '../tasks/task.models';
import { TaskService } from '../tasks/task.service';
import { Board, BoardColumn, BoardSummary, SaveColumnRequest } from './board.models';
import { BoardService } from './board.service';

// State for one open board: its columns, the cards in them and the people who can be
// assigned. Provided by BoardPage, so each visit starts clean.
@Injectable()
export class BoardStore {
  private readonly api = inject(BoardService);
  private readonly tasks = inject(TaskService);
  private readonly projects = inject(ProjectService);
  private readonly labelApi = inject(LabelService);
  private readonly realtime = inject(RealtimeService);

  readonly board = signal<Board | null>(null);
  readonly columns = signal<BoardColumn[]>([]);
  readonly members = signal<TaskMember[]>([]);
  readonly labels = signal<Label[]>([]);
  readonly projectBoards = signal<BoardSummary[]>([]);
  readonly loading = signal(true);
  readonly notFound = signal(false);

  readonly canManage = computed(() => this.board()?.myRole === 'Manager');
  readonly canEditTasks = computed(() => this.board()?.myRole !== 'Viewer');
  readonly dropListIds = computed(() => this.columns().map((column) => columnDropId(column.id)));

  constructor() {
    // Changes other people make arrive over the hub. Our own changes are filtered out
    // by the server, which knows this browser's connection id.
    this.realtime.boardEvents$
      .pipe(takeUntilDestroyed())
      .subscribe((event) => this.applyEvent(event));

    // Anything that happened while the connection was down was missed, so start over.
    this.realtime.reconnected$.pipe(takeUntilDestroyed()).subscribe(() => {
      const board = this.board();
      if (board) {
        this.load(board.id);
      }
    });
  }

  private applyEvent(event: BoardEvent): void {
    const board = this.board();
    if (!board) {
      return;
    }

    switch (event.type) {
      case 'TaskCreated':
      case 'TaskUpdated':
      case 'TaskMoved': {
        const card = event.payload as TaskCard;
        if (card?.id) {
          this.columns.update((columns) => placeCard(columns, card));
        }
        break;
      }
      case 'TaskDeleted':
        this.removeTask(event.payload as number);
        break;
      case 'ColumnsChanged':
        this.load(board.id);
        break;
    }
  }

  load(boardId: number): void {
    this.loading.set(true);
    this.notFound.set(false);

    this.api
      .get(boardId)
      .pipe(
        switchMap((board) =>
          forkJoin({
            board: of(board),
            boards: this.api.listForProject(board.projectId),
            members: this.projects.members(board.projectId),
            labels: this.labelApi.listForProject(board.projectId),
          }),
        ),
        catchError(() => {
          this.notFound.set(true);
          this.loading.set(false);
          return EMPTY;
        }),
      )
      .subscribe(({ board, boards, members, labels }) => {
        this.board.set(board);
        this.columns.set(board.columns);
        this.projectBoards.set(boards);
        this.members.set(
          members.map((m) => ({ id: m.userId, fullName: m.fullName, email: m.email })),
        );
        this.labels.set(labels);
        this.loading.set(false);
      });
  }

  addColumn(request: SaveColumnRequest): void {
    const board = this.board()!;

    this.api
      .addColumn(board.id, request)
      .pipe(ignoreHandledError())
      .subscribe((column) => this.columns.update((columns) => [...columns, column]));
  }

  updateColumn(columnId: number, request: SaveColumnRequest): void {
    this.api
      .updateColumn(columnId, request)
      .pipe(ignoreHandledError())
      // The response has no tasks in it, so the cards already on screen are kept.
      .subscribe((updated) =>
        this.columns.update((columns) =>
          columns.map((c) => (c.id === updated.id ? { ...updated, tasks: c.tasks } : c)),
        ),
      );
  }

  deleteColumn(columnId: number): void {
    this.api
      .deleteColumn(columnId)
      .pipe(ignoreHandledError())
      .subscribe(() => this.columns.update((columns) => columns.filter((c) => c.id !== columnId)));
  }

  // Moves the column on screen first, then saves the new order. If the save fails,
  // the previous order is restored.
  moveColumn(fromIndex: number, toIndex: number): void {
    const board = this.board()!;
    const previous = this.columns();
    const reordered = [...previous];
    reordered.splice(toIndex, 0, ...reordered.splice(fromIndex, 1));
    this.columns.set(reordered);

    this.api
      .reorderColumns(
        board.id,
        reordered.map((c) => c.id),
      )
      .pipe(
        catchError(() => {
          this.columns.set(previous);
          return EMPTY;
        }),
      )
      .subscribe((columns) =>
        this.columns.set(
          columns.map((column) => ({
            ...column,
            tasks: reordered.find((c) => c.id === column.id)?.tasks ?? [],
          })),
        ),
      );
  }

  renameBoard(name: string): void {
    const board = this.board()!;

    this.api
      .rename(board.id, name)
      .pipe(ignoreHandledError())
      .subscribe((updated) => {
        this.board.set(updated);
        this.projectBoards.update((boards) =>
          boards.map((b) => (b.id === updated.id ? { id: updated.id, name: updated.name } : b)),
        );
      });
  }

  addTask(task: TaskDetails): void {
    this.columns.update((columns) =>
      columns.map((column) =>
        column.id === task.columnId
          ? { ...column, tasks: [...column.tasks, toCard(task)] }
          : column,
      ),
    );
  }

  applyTaskChanges(task: TaskDetails, commentCount?: number): void {
    this.columns.update((columns) =>
      columns.map((column) => ({
        ...column,
        tasks: column.tasks.map((card) =>
          card.id === task.id ? toCard(task, commentCount ?? card.commentCount) : card,
        ),
      })),
    );
  }

  removeTask(taskId: number): void {
    this.columns.update((columns) =>
      columns.map((column) => ({ ...column, tasks: column.tasks.filter((t) => t.id !== taskId) })),
    );
  }

  // Called after a card is dropped. The board is updated straight away and the server is
  // told which cards the task ended up between, not which index it landed on.
  moveTask(taskId: number, targetColumnId: number, targetIndex: number): void {
    const previous = this.columns();
    const card = previous.flatMap((c) => c.tasks).find((t) => t.id === taskId);
    if (!card) {
      return;
    }

    const moved = { ...card, columnId: targetColumnId };
    const updated = previous.map((column) => {
      const tasks = column.tasks.filter((t) => t.id !== taskId);
      if (column.id === targetColumnId) {
        tasks.splice(targetIndex, 0, moved);
      }
      return { ...column, tasks };
    });
    this.columns.set(updated);

    const target = updated.find((c) => c.id === targetColumnId)!.tasks;
    const above = target[targetIndex - 1] ?? null;
    const below = target[targetIndex + 1] ?? null;

    this.tasks
      .move(taskId, {
        columnId: targetColumnId,
        aboveTaskId: above?.id ?? null,
        belowTaskId: below?.id ?? null,
      })
      .pipe(
        catchError(() => {
          this.columns.set(previous);
          return EMPTY;
        }),
      )
      .subscribe((saved) =>
        this.columns.update((columns) =>
          columns.map((column) => ({
            ...column,
            tasks: column.tasks.map((t) => (t.id === saved.id ? saved : t)),
          })),
        ),
      );
  }
}

// Puts a card in the right column, in position order, whether it is new or moved.
function placeCard(columns: BoardColumn[], card: TaskCard): BoardColumn[] {
  return columns.map((column) => {
    const others = column.tasks.filter((t) => t.id !== card.id);

    if (column.id !== card.columnId) {
      return { ...column, tasks: others };
    }

    const tasks = [...others, card].sort((a, b) => a.position - b.position || a.id - b.id);
    return { ...column, tasks };
  });
}

export function columnDropId(columnId: number): string {
  return `column-${columnId}`;
}

function toCard(task: TaskDetails, commentCount = 0): TaskCard {
  return {
    id: task.id,
    number: task.number,
    title: task.title,
    priority: task.priority,
    dueDate: task.dueDate,
    columnId: task.columnId,
    position: task.position,
    assignee: task.assignee,
    labels: task.labels,
    hasDescription: !!task.description,
    commentCount,
  };
}
