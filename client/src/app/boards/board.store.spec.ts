import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Board, BoardColumn } from './board.models';
import { BoardStore } from './board.store';

describe('BoardStore', () => {
  let store: BoardStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BoardStore, provideHttpClient(), provideHttpClientTesting()],
    });

    store = TestBed.inject(BoardStore);
    httpMock = TestBed.inject(HttpTestingController);

    store.load(7);
    httpMock.expectOne('/api/boards/7').flush(board());
    httpMock.expectOne('/api/projects/3/boards').flush([{ id: 7, name: 'Development' }]);
  });

  afterEach(() => httpMock.verify());

  function board(): Board {
    return {
      id: 7,
      projectId: 3,
      projectKey: 'CP',
      projectName: 'Customer Portal',
      name: 'Development',
      myRole: 'Manager',
      columns: [
        { id: 1, name: 'To do', position: 1, category: 'ToDo' },
        { id: 2, name: 'In progress', position: 2, category: 'InProgress' },
        { id: 3, name: 'Done', position: 3, category: 'Done' },
      ],
    };
  }

  function columnNames(): string[] {
    return store.columns().map((c) => c.name);
  }

  it('loads the board and its columns', () => {
    expect(columnNames()).toEqual(['To do', 'In progress', 'Done']);
    expect(store.canManage()).toBe(true);
    expect(store.loading()).toBe(false);
  });

  it('moves a column on screen before the save completes', () => {
    store.moveColumn(2, 0);

    expect(columnNames()).toEqual(['Done', 'To do', 'In progress']);

    const request = httpMock.expectOne('/api/boards/7/columns/order');
    expect(request.request.body).toEqual({ columnIds: [3, 1, 2] });
    request.flush(renumbered([3, 1, 2]));
  });

  it('puts the columns back when the save fails', () => {
    store.moveColumn(0, 2);
    expect(columnNames()).toEqual(['In progress', 'Done', 'To do']);

    httpMock
      .expectOne('/api/boards/7/columns/order')
      .flush(null, { status: 409, statusText: 'Conflict' });

    expect(columnNames()).toEqual(['To do', 'In progress', 'Done']);
  });

  function renumbered(ids: number[]): BoardColumn[] {
    const columns = board().columns;
    return ids.map((id, index) => ({ ...columns.find((c) => c.id === id)!, position: index + 1 }));
  }
});
