# CLAUDE.md

we're building the app described @SPEC.MD. Read that file in for general achitecture tasks or to double check the dataBase structure ,technical stack or application achitecture

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is an early-stage ASP.NET Core MVC project. As of now it contains only the default MVC template scaffolding plus the initial `Book`/`ReadingStatus` models and an empty `ApplicationDbContext` — there is no `BooksController`, no service layer, no EF Core migrations, and no test project yet. Consult `SPEC.MD` (full technical spec) before adding features; it defines the target architecture, and most of the codebase described there does not exist yet.

## Commands

```bash
dotnet build                       # build the project
dotnet run                         # run the app (see Properties/launchSettings.json for ports/profiles)
dotnet watch run                   # run with hot reload

dotnet ef migrations add <Name>    # add an EF Core migration (requires dotnet-ef tool)
dotnet ef database update          # apply migrations to create/update the SQLite db

dotnet test                        # run all tests (once a test project exists)
dotnet test --filter FullyQualifiedName~<TestName>   # run a single test
```

The `dotnet-ef` global tool must be installed (`dotnet tool install --global dotnet-ef`) before running migration commands, since `Microsoft.EntityFrameworkCore.Design` is a build-time-only dependency.

There is no `.sln` file — commands operate directly on `ReadingTracker.csproj`.

## Architecture

The target architecture (per `SPEC.MD` §7) is a simple layered MVC structure — **do not introduce additional layers, patterns, or enterprise abstractions** (e.g. repositories on top of EF Core, CQRS, MediatR); the project is explicitly meant to stay small:

```
Controllers/   # thin controllers; delegate business logic to Services
Models/        # Book, ReadingStatus (enum), ErrorViewModel — EF Core maps these directly
Data/          # ApplicationDbContext (EF Core, SQLite)
Services/      # BookService — the business logic layer, designed to be unit-testable without HTTP/UI
Views/         # Razor views
Tests/         # UnitTests/ and IntegrationTests/ (xUnit) — not yet created
```

Key architectural points from the spec:

- **`BookService` is the seam for business logic and unit tests.** Controllers should stay thin and call into `BookService` (`GetBooks`, `SearchBooks`, `GetBook`, `CreateBook`, `UpdateBook`, `DeleteBook`, `ChangeStatus`) rather than querying `ApplicationDbContext` directly.
- **Reading status is a first-class concept.** `ReadingStatus` (`WantToRead` / `CurrentlyReading` / `Completed`) is stored as an int in SQLite. Status changes (e.g. "Start Reading", "Complete") must update the existing `Book` row in place — never create a new record.
- **Books are grouped by status in the UI** (Want to Read / Currently Reading / Completed sections), each with status-appropriate quick-action buttons.
- **Search is read-only** and filters by title/author (case-insensitive) across all statuses; it must not be implemented as a mutating operation.
- **Validation lives on the model** via data annotations (see `Models/Book.cs`): `Title`/`Author`/`Genre` required, `Rating` is optional but constrained to 1–5, `ReadingNotes` capped at 1000 chars.
- **Tests are a primary deliverable, not an afterthought.** This project exists to practice CI/testing workflows (see `SPEC.MD` §12–17): unit tests target `BookService` in isolation; integration tests exercise the full HTTP → Controller → Service → EF Core → SQLite path against a *separate test database*, never the dev database (`readingtracker.db`).
- **No authentication, no external APIs, no JS framework** — these are explicitly out of scope (`SPEC.MD` §20).

## CI

No GitHub Actions workflow exists yet. When added, it must (per `SPEC.MD` §15–17): trigger on pushes/PRs to `main`, restore, build, run unit tests, then run integration tests, failing the pipeline if the build or any test fails.

## Testing intent
No tests exist yet, but every class you write should be mockable/testable
without spinning up a real database or the ASP.NET pipeline. If a design
choice would make future unit/integration testing harder, flag it and
propose the testable alternative instead of silently implementing it.