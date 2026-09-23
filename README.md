# TaskForge

TaskForge is a project and task management app for small teams. Work is organized into organizations, projects and kanban boards, and changes made by one person show up for everyone viewing the same board without a refresh.

> Work in progress. This README grows as features land.

## Features

Planned for the first version:

- Registration, login and JWT authentication with refresh tokens
- Organizations and projects with member roles
- Kanban boards with custom columns and drag-and-drop ordering
- Tasks with assignee, priority, due date, labels, comments and attachments
- Activity history for every task
- Notifications for assignments and comments
- Search and filtering
- Dashboard with workload and status overview
- Real-time board updates with SignalR

## Technology stack

| Area | Technologies |
|---|---|
| Backend | C#, .NET 8, ASP.NET Core Web API, Entity Framework Core, SQL Server, SignalR, Serilog, Swagger |
| Frontend | Angular 21, TypeScript, RxJS, Angular Material, Angular CDK |
| Testing | xUnit, ASP.NET Core integration tests, Vitest |

## Repository layout

```
server/TaskForge.Api/      ASP.NET Core Web API
tests/TaskForge.Api.Tests/ xUnit tests
client/                    Angular application
```

## Boards

Every project starts with a board containing four columns: To do, In progress, Testing and Done. Managers can rename, add, reorder (drag and drop) and delete columns, and a project can have several boards.

Each column has a type (`ToDo`, `InProgress`, `Done`) separate from its name, so teams can rename columns freely while reports still know what counts as finished. Reordering sends the complete list of column ids rather than "moved column X to position 3", which keeps the result predictable when two people reorder at the same time. A column or board that still holds tasks can't be deleted, and a project always keeps at least one board.

## Tasks and the kanban board

Tasks get a project-scoped key such as `CP-12`, taken from a counter on the project that is protected by a concurrency token, so two tasks created at the same moment can't share a number.

Cards are dragged within a column and between columns. Instead of an index, the client tells the API which cards the task was dropped **between**, and the server gives it the midpoint of their positions, so a move updates a single row and stays correct if someone else rearranged the column in the meantime. When repeated drops in the same spot leave no room between two neighbours, the column is spread out evenly again.

Editing a task sends back the row version it was loaded with. If someone else saved first, the API answers `409` and the panel offers to reload their version instead of silently overwriting it. Moving a card into a `Done` column marks the task completed; moving it out reopens it. Every change is written to the activity history.

## Real-time updates

A single SignalR hub at `/hubs/app` keeps open boards in sync. A browser joins the group of the board it is showing and leaves it on navigation, so events only reach people actually looking at that board. Membership is checked again when joining, because reading a group is reading other people's work.

Events are sent from the services after the change is saved, and the browser that made the change is left out: it passes its connection id in an `X-Connection-Id` header and the server sends the event to the rest of the group. Notifications go to a user rather than a group, so they arrive wherever that person is in the app.

Browsers can't set headers on a WebSocket, so the access token travels in the query string for hub requests only. After a dropped connection the client rejoins its board and reloads, because events that happened while it was offline are not replayed.

Notifications are stored in the database and pushed live: being assigned a task, a comment on a task you reported or are assigned to, and being added to a project. Nobody is notified about their own actions.

## Collaboration

Each task panel has tabs for details, comments, files and history.

- **Labels** belong to a project, are unique by name within it, and show on the cards. Deleting one takes it off every task that used it.
- **Comments** are paged, can be edited by their author, and deleted by the author or a project manager. Viewers may comment even though they can't change tasks.
- **History** records every change to a task — created, renamed, reassigned, moved, priority and due date changes, comments and uploads — and the project page shows the latest entries across all its tasks.
- **Attachments** are stored on disk under `App_Data/uploads` with a generated name, never the uploaded one. Files are limited to 10 MB and to an allow-list of types, and downloads go through the API so permissions are checked.

Times are stored in UTC and sent as UTC timestamps, so the browser shows them in the reader's own timezone.

## Roles and permissions

Work is organized as organization → projects → boards → tasks. Access is checked on the server for every request, using roles stored in the database rather than in the token, so a role change takes effect immediately.

| Organization role | Can |
|---|---|
| Owner | Everything, including deleting the organization and managing owners |
| Admin | Manage members and projects; counts as manager of every project |
| Member | See the organization and the projects they belong to |

| Project role | Can |
|---|---|
| Manager | Project settings, members, boards and columns |
| Contributor | Create, edit and move tasks; comment |
| Viewer | Read and comment |

People who aren't members get `404` instead of `403`, so they can't discover which organizations or projects exist.

## Authentication

- `POST /api/auth/register` and `POST /api/auth/login` return a short-lived JWT access token (15 minutes) in the response body.
- The refresh token is sent as an `HttpOnly`, `Secure`, `SameSite=Strict` cookie scoped to `/api/auth`, so scripts in the page can't read it.
- `POST /api/auth/refresh` exchanges the cookie for a new access token and rotates the refresh token. Each refresh token works once and is stored only as a SHA-256 hash.
- `POST /api/auth/logout` revokes the refresh token and clears the cookie.
- Every other endpoint requires a valid access token unless it is explicitly marked anonymous.

On the client, the access token is kept in memory only. When the app starts it calls `/api/auth/refresh` to restore the session from the cookie, so a page reload doesn't sign the user out. An HTTP interceptor attaches the token to API calls and, when a call fails with 401, refreshes the token once and retries. Concurrent failures share a single refresh request.

In Swagger, call `/api/auth/login`, copy `accessToken`, and paste it into the **Authorize** dialog.

## Getting started

### Prerequisites

- .NET SDK 8 or later (the API targets `net8.0`)
- Node.js 22.12 or later
- SQL Server or SQL Server LocalDB

### Run the API

```bash
cd server/TaskForge.Api
dotnet run
```

The API listens on `http://localhost:5080`. Swagger UI is at `http://localhost:5080/swagger`.

In the Development environment the API applies EF Core migrations on startup and seeds a demo workspace into the `TaskForge` database on LocalDB. The connection string is in `appsettings.json`. Demo accounts (password `Demo@1234`):

| Email | Role |
|---|---|
| sarah@example.com | Organization owner |
| daniel@example.com | Organization admin, project manager |
| priya@example.com | Member, contributor |
| tom@example.com | Member, viewer on Customer Portal |

The JWT signing key for development is in `appsettings.Development.json`. In any other environment the API refuses to start until `Jwt:Key` is set, for example via the `Jwt__Key` environment variable or user secrets.

To add a migration after changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project server/TaskForge.Api --output-dir Data/Migrations
```

### Run the client

```bash
cd client
npm install
npm start
```

Open `http://localhost:4200`. Requests to `/api` and `/hubs` are proxied to the API (see `client/proxy.conf.json`), so no CORS setup is needed during development.

### Run the tests

```bash
dotnet test
```

API tests run against a separate `TaskForge_Tests` database that is recreated on every run. Set the `TASKFORGE_TEST_DB` environment variable to use a different SQL Server connection string.

```bash
cd client
npm test
```

## License

[MIT](LICENSE)
