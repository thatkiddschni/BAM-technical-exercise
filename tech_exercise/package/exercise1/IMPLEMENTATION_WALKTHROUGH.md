# ACTS Technical Exercise: Implementation Walkthrough

## History and scope

The repository history contains three commits through the current `acts-exercise` branch:

| Commit | Date | Summary |
|---|---|---|
| `1c403bf` | 2025-09-17 | Initial commit |
| `e85688e` | 2025-09-17 | add exercise |
| `6168176` | 2026-10-28 | acts exercise genesis |

The last commit, `6168176`, is the starting point for the ACTS exercise. There are no later commits in the current branch history. The API changes, API tests, and UI described below are present in the current workspace but are not part of a later commit: the API and test changes are staged, the README update is unstaged, and the Angular UI is untracked. Generated files and incidental artifacts are not included in this account.

The exercise brief describes the Astronaut Career Tracking System (ACTS), which keeps a master list of people, current astronaut details, and each astronaut's duty history. Its required API operations are person lookup/list/create-update and astronaut-duty lookup/create. The brief also asks for the duty and retirement rules to be enforced, defensive coding, unit tests targeting more than 50% coverage, and database-persisted success and exception logs. A polished Angular UI is encouraged. See the [exercise brief](./README.md) and the [repository overview](../README.md).

## Starting point: `6168176` — acts exercise genesis

At genesis, the exercise contained an ASP.NET Core API using .NET 8, Entity Framework Core with SQLite, Dapper, and MediatR. The initial data model and migration covered `Person`, `AstronautDetail`, and `AstronautDuty`; `AstronautDetail` was related one-to-one with a person and duties one-to-many. The migration did not yet enforce unique person names.

The API already had MediatR query and command handlers for listing and looking up people, creating people, retrieving duties, and creating a duty. The duty creation preprocessor and handler were synchronous in places and had limited validation. In particular, its SQL interpolated the supplied name directly, and the preprocessor checked for a duplicate duty title and start date without scoping that check to a person. Retirement handling and duty-date handling did not consistently match the exercise rules. Startup registered the database context and MediatR, but did not apply migrations or seed data.

There was no test project or Angular UI at this point. The starting API, models, and initial migration are visible in the files committed at `6168176`.

## Changes in the current workspace

### 1. Data access and database initialization

The entity classes and `StargateContext` were moved from `Business/Data` into `DataAccess`, separating persistence types from business handlers. The existing initial migration moved with them, and a current EF Core model snapshot was added. The application now registers a context factory, applies pending migrations at startup, and seeds sample data only when the people table is empty. The seed contains two people, with one astronaut record, so the directory also demonstrates a person without astronaut duties. See [Program.cs](./api/Program.cs) and [StargateContext.cs](./api/DataAccess/StargateContext.cs).

Two migrations extend the database:

- `AddUniquePersonName` adds a unique index on `Person.Name`, enforcing the exercise's identity rule in SQLite, not just in a request handler.
- `AddProcessLogs` creates the table used to persist operation outcomes and exception information.

These are in [Migrations](./api/DataAccess/Migrations/).

### 2. Person creation, updates, and defensive API behavior

Person creation now uses the EF Core context and translates SQLite's unique-constraint error into a conflict response. A new `UpdatePersonById` command changes a person's name and returns not-found or conflict results as appropriate. The corresponding controller action is a `PATCH` endpoint.

The person-lookup and person-update actions validate names (required and at most 50 characters); the update action also rejects non-positive IDs. Duty creation validates its request, name, rank, title, and start date before dispatching the command. Missing people and invalid requests are returned as explicit API responses rather than relying on exceptions for ordinary validation failures. The API also uses named routes for the people and duty operations consumed by the UI. See [PersonController.cs](./api/Controllers/PersonController.cs) and [AstronautDutyController.cs](./api/Controllers/AstronautDutyController.cs).

The duty write path now uses EF Core lookups rather than interpolated SQL. Query handlers use parameterized Dapper commands where applicable. These changes avoid building SQL from user-supplied names and make missing-person results explicit.

### 3. Duty history and career rules

`CreateAstronautDutyHandler` now applies the duty rules when recording an assignment:

- It finds the person by name and returns not found if there is no match.
- It normalizes the incoming start date to its date component and rejects a date that is not later than the person's latest duty start date.
- It updates the existing current-duty details or creates them for a person's first assignment.
- When a new duty is added, the previous duty's end date becomes the day before the new start date. The new duty has no end date, so it remains the current assignment.
- The duty title `RETIRED` is detected case-insensitively. A retirement duty sets the career end date to the day before its start date; a non-retirement first duty starts the career on its own start date.

This replaces the genesis implementation's broader duplicate-title/date check with a per-person chronological rule, and fixes the first-duty retirement end-date calculation. See [CreateAstronautDuty.cs](./api/Business/Commands/CreateAstronautDuty.cs).

### 4. Persisted process logging

`ProcessLog` records an operation, component, outcome, message, optional exception type/details, and UTC timestamp. `ProcessLogger` writes entries through a context factory and provides a read-only listing operation. A MediatR pipeline behavior logs command/query success or failure results and logs then rethrows handler exceptions. Controllers also log request outcomes and caught exceptions. The log endpoint is implemented by [ProcessLoggerController.cs](./api/Controllers/ProcessLoggerController.cs).

The Angular log view deliberately displays ordinary log fields without exposing exception details. The API entity and UI model therefore serve different purposes: full exception details are persisted server-side, while the UI presents the operational summary.

### 5. API test coverage

The new .NET test project uses xUnit, Moq, and an in-memory SQLite database. Tests cover business handlers, controller validation and response mapping, process logging behavior, and persisted log retrieval. The principal files are in [api.tests](./api.tests/).

In this workspace, `dotnet test api.tests/StargateAPI.Tests.csproj` passed all 24 tests. Running with `--collect:"XPlat Code Coverage"` also passed all 24 tests and produced a Cobertura report with **52.94% line coverage** (531 of 1,003 lines) and **85.36% branch coverage** (70 of 82 branches). The measured line coverage exceeds the exercise brief's stated 50% target.

### 6. Angular UI

An Angular application was added under [ui](./ui/). It has four workspaces: flight-record search, the people directory, duty creation, and process logs. It calls the API through typed HTTP services, encodes names in lookup URLs, and validates form inputs. The duty-search experience distinguishes loading, success, no duties, not found, and request-error states; the other workspaces provide corresponding operation feedback.

The development proxy forwards `/api` to the API's `http` launch profile at `http://localhost:5204`. The environment config sets the API base URL to `/api`. Angular component and service tests cover request wiring, validation, result states, mutations, and the omission of exception details from the log display.

## Running and verifying the current snapshot

From the exercise directory:

```sh
dotnet test api.tests/StargateAPI.Tests.csproj
dotnet test api.tests/StargateAPI.Tests.csproj --collect:"XPlat Code Coverage"
```

Start the API using the `http` launch profile (`http://localhost:5204`), then, from the UI directory, install dependencies and start the development server:

```sh
cd ui
npm install
npm start
```

From `ui/`, the UI tests and production build are:

```sh
npm test
npm run build
```

These commands and the proxy/base-URL details are also documented in the [exercise README](./README.md). In this workspace, `npm test` passed all 15 tests, and `npm run build` completed successfully.

## Current state and caveats

The workspace now contains the API rule enforcement, name uniqueness and person update support, persisted process logs, API and UI tests, and an Angular client. The UI validates new-person names, but the API's `CreatePerson` action does not apply the same explicit empty/length checks as lookup and update. The verification results above apply to the current workspace changes, not to a later Git commit: `6168176` remains `HEAD`, and the implementation changes described here are still staged, unstaged, or untracked as noted above.

The exercise brief's >50% coverage objective was measured for the API test suite; no separate UI coverage percentage was collected. The UI tests and production build were run, but this walkthrough does not claim a live end-to-end session against a running API.
