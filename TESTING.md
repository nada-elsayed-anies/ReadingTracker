# TESTING.md

Testing strategy for the Personal Reading Tracker. For the day-to-day rules Claude Code should follow when
writing tests here, see `.claude/skills/dotnet-testing/SKILL.md`; this file documents the strategy and what
currently exists.

## Strategy: two levels

Per `SPEC.MD` §12–14, testing is split into unit tests (business logic in isolation) and integration tests
(the full HTTP → Controller → Service → EF Core → SQLite path). Both use xUnit and both follow
**Arrange → Act → Assert**, with each test independent of the others (no shared mutable state, no ordering
dependencies).

### What belongs in unit tests (`Tests/UnitTests`)

- `BookService` behavior: CRUD, ordering, status changes, search (including null/empty search term fallback).
- `Book` model validation rules (required fields, rating range, notes length).
- Nothing that requires HTTP, routing, or the ASP.NET Core pipeline.

### What belongs in integration tests (`Tests/IntegrationTests`)

- End-to-end flows through real HTTP requests: form POSTs, redirects, status codes.
- Verifying the database was actually persisted/updated, not just that the HTTP response looked right.

## SQLite strategy

The two test levels intentionally use SQLite differently:

- **Unit tests** (`SqliteInMemoryTestBase`) open one `SqliteConnection("DataSource=:memory:")` per test class instance and keep it open for the test's lifetime (SQLite's `:memory:` mode only persists while a connection is open), then call `Context.Database.EnsureCreated()` — fast, schema built directly from the model. Deliberately uses the real `Microsoft.Data.Sqlite` provider, not EF Core's `UseInMemoryDatabase`, because in-memory-provider behavior diverges from real SQLite (e.g. constraints, `LIKE`).
- **Integration tests** (`ReadingTrackerWebApplicationFactory`) use a real temp SQLite **file** (unique per test method, in the OS temp directory) and apply the actual EF Core migrations via `Database.MigrateAsync()` — closer to how the schema reaches production. Never the dev database (`readingtracker.db`).

## Integration test infrastructure

`ReadingTrackerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime`:

- `ConfigureWebHost` replaces `DbContextOptions<ApplicationDbContext>` (pointing at the temp file DB) and `IAntiforgery` (a no-op `DisabledAntiforgery`).
- `InitializeAsync()` runs `Database.MigrateAsync()` against the fresh file.
- `DisposeAsync()` calls `SqliteConnection.ClearAllPools()` before deleting the temp file (plus its `-shm`/`-wal` siblings) — required because pooled connections keep the file locked otherwise.
- Test classes implement `IAsyncLifetime` (not `IClassFixture`), creating their own factory/`HttpClient` per test method, so every test method gets a fresh, isolated database. `BooksControllerIntegrationTests` follows this pattern.

## Anti-forgery testing approach

`[ValidateAntiForgeryToken]` stays on every mutating `BooksController` action in production code, unchanged.
The test factory's `DisabledAntiforgery` substitution exists only so integration tests can POST forms without
scraping antiforgery tokens — it's used because the point of those tests is controller/service/EF Core
integration, not antiforgery itself. A test that specifically needs to verify antiforgery behavior should not
rely on this substitution.

## Current coverage

- **Unit**: `BookServiceTests` (CRUD, ordering, status change, search incl. no-match/empty-term cases) and `BookValidationTests` (required fields, rating range/boundary, notes max length).
- **Integration**: `BooksControllerIntegrationTests` — create-persists-to-DB, index-returns-expected-books, complete-changes-status-in-DB, delete-removes-from-DB.

## CI

`dotnet test` for both projects now runs in GitHub Actions on every push/PR to `main`, via `.github/workflows/ci.yml` (see `PLAN.md` and `CLAUDE.md`'s CI section).

## Future testing goals

- Revisit the integration factory's `ClearAllPools()` teardown if a second integration test class is added (currently process-wide, noted as a potential race in `TODO.md`; not an issue yet with a single test class).

## Commands

```bash
dotnet build
dotnet test
dotnet test Tests/UnitTests/ReadingTracker.UnitTests.csproj
dotnet test Tests/IntegrationTests/ReadingTracker.IntegrationTests.csproj
dotnet test --filter FullyQualifiedName~<TestName>
```

See `CLAUDE.md`'s Commands section for EF Core migration commands and other project commands.

If a test fails, investigate and fix the root cause — do not weaken or delete the test to make it pass.
