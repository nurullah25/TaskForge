import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Board, BoardColumn, BoardSummary, SaveColumnRequest } from './board.models';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private readonly http = inject(HttpClient);

  listForProject(projectId: number): Observable<BoardSummary[]> {
    return this.http.get<BoardSummary[]>(`/api/projects/${projectId}/boards`);
  }

  get(boardId: number): Observable<Board> {
    return this.http.get<Board>(`/api/boards/${boardId}`);
  }

  create(projectId: number, name: string): Observable<Board> {
    return this.http.post<Board>(`/api/projects/${projectId}/boards`, { name });
  }

  rename(boardId: number, name: string): Observable<Board> {
    return this.http.put<Board>(`/api/boards/${boardId}`, { name });
  }

  delete(boardId: number): Observable<void> {
    return this.http.delete<void>(`/api/boards/${boardId}`);
  }

  addColumn(boardId: number, request: SaveColumnRequest): Observable<BoardColumn> {
    return this.http.post<BoardColumn>(`/api/boards/${boardId}/columns`, request);
  }

  updateColumn(columnId: number, request: SaveColumnRequest): Observable<BoardColumn> {
    return this.http.put<BoardColumn>(`/api/columns/${columnId}`, request);
  }

  deleteColumn(columnId: number): Observable<void> {
    return this.http.delete<void>(`/api/columns/${columnId}`);
  }

  reorderColumns(boardId: number, columnIds: number[]): Observable<BoardColumn[]> {
    return this.http.put<BoardColumn[]>(`/api/boards/${boardId}/columns/order`, { columnIds });
  }
}
