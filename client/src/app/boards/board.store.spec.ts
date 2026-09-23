import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaskCard } from '../tasks/task.models';
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
    httpMock
      .expectOne('/api/projects/3/members')
      .flush([
        {
          userId: 5,
          fullName: 'Sarah Khan',
          email: 'sarah@example.com',
          role: 'Manager',
          addedAt: '',
        },
      ]);
  });

  afterEach(() => httpMock.verify());

  function card(id: number, title: string, columnId: number, position: number): TaskCard {
    return {
      id,
      number: id,
      title,
      priority: 'Medium',
      dueDate: null,
      columnId,
      position,
      assignee: null,
      hasDescription: false,
      commentCount: 0,
    };
  }

  function board(): Board {
    return {
      id: 7,
      projectId: 3,
      projectKey: 'CP',
      projectName: 'Customer Portal',
      name: 'Development',
      myRole: 'Manager',
      columns: [
        {
          id: 1,
          name: 'To do',
          position: 1,
          category: 'ToDo',
          tasks: [
            card(10, 'First', 1, 1000),
            card(11, 'Second', 1, 2000),
            card(12, 'Third', 1, 3000),
          ],
        },
        { id: 2, name: 'In progress', position: 2, category: 'InProgress', tasks: [] },
        { id: 3, name: 'Done', position: 3, category: 'Done', tasks: [] },
      ],
    };
  }

  function titlesIn(columnId: number): string[] {
    return store
      .columns()
      .find((c) => c.id === columnId)!
      .tasks.map((t) => t.title);
  }

  it('loads columns, cards and the people who can be assigned', () => {
    expect(titlesIn(1)).toEqual(['First', 'Second', 'Third']);
    expect(store.members()).toEqual([
      { id: 5, fullName: 'Sarah Khan', email: 'sarah@example.com' },
    ]);
    expect(store.dropListIds()).toEqual(['column-1', 'column-2', 'column-3']);
  });

  it('tells the server which cards a dropped task ended up between', () => {
    // "Third" is dropped between "First" and "Second".
    store.moveTask(12, 1, 1);

    expect(titlesIn(1)).toEqual(['First', 'Third', 'Second']);

    const request = httpMock.expectOne('/api/tasks/12/move');
    expect(request.request.body).toEqual({ columnId: 1, aboveTaskId: 10, belowTaskId: 11 });
    request.flush({ ...card(12, 'Third', 1, 1500) });
  });

  it('sends no neighbours when a task is dropped into an empty column', () => {
    store.moveTask(10, 2, 0);

    expect(titlesIn(1)).toEqual(['Second', 'Third']);
    expect(titlesIn(2)).toEqual(['First']);

    const request = httpMock.expectOne('/api/tasks/10/move');
    expect(request.request.body).toEqual({ columnId: 2, aboveTaskId: null, belowTaskId: null });
    request.flush({ ...card(10, 'First', 2, 1000) });
  });

  it('puts the card back where it was when the move fails', () => {
    store.moveTask(10, 3, 0);
    expect(titlesIn(3)).toEqual(['First']);

    httpMock.expectOne('/api/tasks/10/move').flush(null, { status: 409, statusText: 'Conflict' });

    expect(titlesIn(1)).toEqual(['First', 'Second', 'Third']);
    expect(titlesIn(3)).toEqual([]);
  });

  it('moves a column on screen before the save completes', () => {
    store.moveColumn(2, 0);

    expect(store.columns().map((c) => c.name)).toEqual(['Done', 'To do', 'In progress']);

    const request = httpMock.expectOne('/api/boards/7/columns/order');
    expect(request.request.body).toEqual({ columnIds: [3, 1, 2] });
    request.flush(renumbered([3, 1, 2]));

    // Cards stay with their column after the server answers.
    expect(titlesIn(1)).toEqual(['First', 'Second', 'Third']);
  });

  it('puts the columns back when the save fails', () => {
    store.moveColumn(0, 2);
    expect(store.columns().map((c) => c.name)).toEqual(['In progress', 'Done', 'To do']);

    httpMock
      .expectOne('/api/boards/7/columns/order')
      .flush(null, { status: 409, statusText: 'Conflict' });

    expect(store.columns().map((c) => c.name)).toEqual(['To do', 'In progress', 'Done']);
  });

  function renumbered(ids: number[]): BoardColumn[] {
    const columns = board().columns;
    return ids.map((id, index) => ({
      ...columns.find((c) => c.id === id)!,
      position: index + 1,
      tasks: [],
    }));
  }
});
