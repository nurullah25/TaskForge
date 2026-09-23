import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, EMPTY, forkJoin, of, switchMap, tap } from 'rxjs';
import { Board, BoardColumn, BoardSummary, SaveColumnRequest } from './board.models';
import { BoardService } from './board.service';

// State for one open board. Provided by BoardPage, so each visit starts clean.
// Phase 6 adds tasks and drag-and-drop between columns on top of this.
@Injectable()
export class BoardStore {
  private readonly api = inject(BoardService);

  readonly board = signal<Board | null>(null);
  readonly columns = signal<BoardColumn[]>([]);
  readonly projectBoards = signal<BoardSummary[]>([]);
  readonly loading = signal(true);
  readonly notFound = signal(false);

  readonly canManage = computed(() => this.board()?.myRole === 'Manager');

  load(boardId: number): void {
    this.loading.set(true);
    this.notFound.set(false);

    this.api
      .get(boardId)
      .pipe(
        switchMap((board) =>
          forkJoin({ board: of(board), boards: this.api.listForProject(board.projectId) }),
        ),
        catchError(() => {
          this.notFound.set(true);
          this.loading.set(false);
          return EMPTY;
        }),
      )
      .subscribe(({ board, boards }) => {
        this.board.set(board);
        this.columns.set(board.columns);
        this.projectBoards.set(boards);
        this.loading.set(false);
      });
  }

  addColumn(request: SaveColumnRequest): void {
    const board = this.board()!;

    this.api
      .addColumn(board.id, request)
      .subscribe((column) => this.columns.update((columns) => [...columns, column]));
  }

  updateColumn(columnId: number, request: SaveColumnRequest): void {
    this.api
      .updateColumn(columnId, request)
      .subscribe((updated) =>
        this.columns.update((columns) => columns.map((c) => (c.id === updated.id ? updated : c))),
      );
  }

  deleteColumn(columnId: number): void {
    this.api
      .deleteColumn(columnId)
      .subscribe(() => this.columns.update((columns) => columns.filter((c) => c.id !== columnId)));
  }

  // Moves the column on screen first, then saves the new order. If the save fails,
  // the board is reloaded so what the user sees matches the server again.
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
        tap((columns) => this.columns.set(columns)),
      )
      .subscribe();
  }

  renameBoard(name: string): void {
    const board = this.board()!;

    this.api.rename(board.id, name).subscribe((updated) => {
      this.board.set(updated);
      this.projectBoards.update((boards) =>
        boards.map((b) => (b.id === updated.id ? { id: updated.id, name: updated.name } : b)),
      );
    });
  }
}
