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

```bash
cd client
npm test
```

## License

[MIT](LICENSE)
