import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export interface BoardEvent {
  type: 'TaskCreated' | 'TaskUpdated' | 'TaskMoved' | 'TaskDeleted' | 'ColumnsChanged';
  payload: unknown;
}

const BOARD_EVENTS: BoardEvent['type'][] = [
  'TaskCreated',
  'TaskUpdated',
  'TaskMoved',
  'TaskDeleted',
  'ColumnsChanged',
];

// One hub connection for the whole app: it starts when the user signs in and stops on
// sign-out. Pages subscribe to the streams they care about.
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private joinedBoardId: number | null = null;

  private readonly boardEventSubject = new Subject<BoardEvent>();
  private readonly notificationSubject = new Subject<unknown>();
  private readonly reconnectedSubject = new Subject<void>();
  private readonly connectionId = signal<string | null>(null);

  readonly boardEvents$: Observable<BoardEvent> = this.boardEventSubject.asObservable();
  readonly notifications$: Observable<unknown> = this.notificationSubject.asObservable();

  // Events sent while the connection was down are not replayed, so pages reload instead.
  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();

  // Sent with API calls so the server doesn't echo our own changes back to us.
  readonly currentConnectionId = computed(() => this.connectionId());

  private readonly userId = computed(() => this.auth.currentUser()?.id ?? null);

  constructor() {
    effect(() => {
      const userId = this.userId();
      untracked(() => (userId === null ? this.stop() : this.start()));
    });
  }

  async joinBoard(boardId: number): Promise<void> {
    this.joinedBoardId = boardId;
    await this.invokeWhenConnected('JoinBoard', boardId);
  }

  async leaveBoard(boardId: number): Promise<void> {
    if (this.joinedBoardId === boardId) {
      this.joinedBoardId = null;
    }

    await this.invokeWhenConnected('LeaveBoard', boardId);
  }

  private start(): void {
    if (this.connection) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/app', { accessTokenFactory: () => this.auth.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    for (const type of BOARD_EVENTS) {
      connection.on(type, (payload: unknown) => this.boardEventSubject.next({ type, payload }));
    }

    connection.on('NotificationReceived', (payload: unknown) =>
      this.notificationSubject.next(payload),
    );

    // After a dropped connection the server no longer knows which board we were on.
    connection.onreconnected((connectionId) => {
      this.connectionId.set(connectionId ?? null);
      if (this.joinedBoardId !== null) {
        void connection.invoke('JoinBoard', this.joinedBoardId);
      }

      this.reconnectedSubject.next();
    });

    this.connection = connection;
    connection
      .start()
      .then(() => this.connectionId.set(connection.connectionId))
      .catch(() => this.connectionId.set(null));
  }

  private stop(): void {
    const connection = this.connection;
    this.connection = null;
    this.joinedBoardId = null;
    this.connectionId.set(null);

    void connection?.stop();
  }

  private async invokeWhenConnected(method: string, ...args: unknown[]): Promise<void> {
    const connection = this.connection;
    if (!connection) {
      return;
    }

    if (connection.state !== HubConnectionState.Connected) {
      // The page may open before the connection is up; the call is retried on reconnect.
      return;
    }

    try {
      await connection.invoke(method, ...args);
    } catch {
      // Losing live updates shouldn't break the page; the data is still loaded over HTTP.
    }
  }
}
