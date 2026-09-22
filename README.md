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
