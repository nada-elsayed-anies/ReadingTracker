# CLAUDE.md

we're building the app described @SPEC.MD. Read that file in for general achitecture tasks or to double check the dataBase structure ,technical stack or application achitecture

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is an early-stage ASP.NET Core MVC project. Book CRUD, reading-status changes, and search are implemented end-to-end (`BooksController` → `IBookService`/`BookService` → `ApplicationDbContext`). Both test projects exist: `Tests/UnitTests` covers `BookService` and `Book` validation directly, and `Tests/IntegrationTests` exercises the full HTTP → Controller → Service → EF Core → SQLite path via `WebApplicationFactory<Program>`. Still missing relative to `SPEC.MD`: the GitHub Actions CI workflow (§15–17). Consult `SPEC.MD` before adding features; it defines the target architecture and remains the source of truth for anything not yet built.

## Commands

```bash
dotnet build                       # build the whole solution (app + tests)
dotnet run                         # run the app (see Properties/launchSettings.json for ports/profiles)
dotnet watch run                   # run with hot reload

dotnet ef migrations add <Name>    # add an EF Core migration (requires dotnet-ef tool)
dotnet ef database update          # apply migrations to create/update the SQLite db

dotnet test                                                                  # run all tests (unit + integration)
dotnet test Tests/UnitTests/ReadingTracker.UnitTests.csproj                 # run just the unit test project
dotnet test Tests/IntegrationTests/ReadingTracker.IntegrationTests.csproj   # run just the integration test project
dotnet test --filter FullyQualifiedName~<TestName>                          # run a single test
```

The `dotnet-ef` global tool must be installed (`dotnet tool install --global dotnet-ef`) before running migration commands, since `Microsoft.EntityFrameworkCore.Design` is a build-time-only dependency.

`ReadingTracker.slnx` is the solution file (XML-based `.slnx` format, not the classic `.sln`) and includes `ReadingTracker.csproj`, `Tests/UnitTests/ReadingTracker.UnitTests.csproj`, and `Tests/IntegrationTests/ReadingTracker.IntegrationTests.csproj`. `ReadingTracker.csproj` explicitly excludes `Tests/**` from its own compile/content globs, so the test projects don't collide when building from the repo root.

## Architecture

The architecture (per `SPEC.MD` §7) is a simple layered MVC structure — **do not introduce additional layers, patterns, or enterprise abstractions** (e.g. repositories on top of EF Core, CQRS, MediatR); the project is explicitly meant to stay small:

```
Controllers/   # BooksController, HomeController — thin, delegate to Services
Models/        # Book, ReadingStatus (enum), ReadingStatusExtensions, ErrorViewModel — EF Core maps these directly
Data/          # ApplicationDbContext (EF Core, SQLite)
Services/      # IBookService / BookService — the business logic layer, unit-testable without HTTP/UI
Migrations/    # EF Core migrations
Views/         # Razor views
Tests/
  UnitTests/        # xUnit; targets BookService + Book validation directly (see SqliteInMemoryTestBase)
  IntegrationTests/ # xUnit; full HTTP → Controller → Service → EF Core → SQLite path (see ReadingTrackerWebApplicationFactory)
```

Key architectural points:

- **`BookService` is the seam for business logic and unit tests.** `BooksController` only calls `IBookService` (`GetBooksAsync`, `SearchBooksAsync`, `GetBookAsync`, `CreateBookAsync`, `UpdateBookAsync`, `DeleteBookAsync`, `ChangeStatusAsync`) — never `ApplicationDbContext` directly. Keep new business logic in `BookService`, not the controller.
- **Reading status is a first-class concept.** `ReadingStatus` (`WantToRead` / `CurrentlyReading` / `Completed`) is stored as an int in SQLite. `ChangeStatusAsync` (used by the controller's `StartReading`/`Complete` actions) updates the existing `Book` row in place and stamps `UpdatedAt` — never create a new record for a status change.
- **Search is read-only.** `SearchBooksAsync` filters by title/author (case-insensitive, via `EF.Functions.Like`) across all statuses, falling back to `GetBooksAsync` when the term is null/whitespace; it must not be implemented as a mutating operation.
- **Validation lives on the model** via data annotations on `Models/Book.cs`: `Title`/`Author`/`Genre` required, `Rating` optional but constrained to 1–5, `ReadingNotes` capped at 1000 chars. Controllers rely on `ModelState.IsValid` rather than re-implementing checks.
- **Unit tests use a real in-memory SQLite connection, not mocks.** `Tests/UnitTests/SqliteInMemoryTestBase` opens a `DataSource=:memory:` `SqliteConnection` and calls `EnsureCreated()` per test class — this is deliberate (SQLite's `:memory:` provider behaves differently from EF's InMemory provider for things like unique constraints), so follow this pattern for new `BookService` tests rather than swapping in `UseInMemoryDatabase`.
- **Tests are a primary deliverable, not an afterthought.** This project exists to practice CI/testing workflows (`SPEC.MD` §12–17). `Tests/IntegrationTests/ReadingTrackerWebApplicationFactory` is a custom `WebApplicationFactory<Program>` (requires the `public partial class Program { }` marker at the bottom of `Program.cs`, since top-level-statement `Program` is otherwise `internal`) that points `ApplicationDbContext` at a fresh temp-file SQLite DB per test method — never the dev database (`readingtracker.db`) — and runs the real EF Core migration (`Database.MigrateAsync()`) against it. It also swaps in a no-op `IAntiforgery` so tests can POST forms without scraping `[ValidateAntiForgeryToken]` tokens. When adding new integration tests, follow this per-test-method isolation pattern (implement `IAsyncLifetime` on the test class, not `IClassFixture`) and remember `SqliteConnection.ClearAllPools()` must run before deleting the temp DB file in teardown, or the file stays locked.
- **No authentication, no external APIs, no JS framework** — explicitly out of scope (`SPEC.MD` §20).

## CI

No GitHub Actions workflow exists yet. When added, it must (per `SPEC.MD` §15–17): trigger on pushes/PRs to `main`, restore, build, run unit tests, then run integration tests, failing the pipeline if the build or any test fails.

## Testing intent
Every class should stay mockable/testable without spinning up a real database or the ASP.NET pipeline — `BookService` takes `ApplicationDbContext` directly because tests supply a real (in-memory SQLite) one, not a mock, per the pattern above. If a design choice would make future unit/integration testing harder, flag it and propose the testable alternative instead of silently implementing it.