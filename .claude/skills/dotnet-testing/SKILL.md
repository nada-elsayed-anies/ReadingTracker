---
name: dotnet-testing
description: How to write and run automated tests (unit + integration) for the Personal Reading Tracker ASP.NET Core MVC app
---

# .NET Testing — Personal Reading Tracker

## When to use this skill

Use this skill whenever you are writing, modifying, or running tests in this repository — new `BookService` behavior, a new controller action, a bug fix that needs regression coverage, or when asked to validate a change with `dotnet build` / `dotnet test`. It applies to both `Tests/UnitTests` and `Tests/IntegrationTests`.

The architecture under test is: `Controllers` → `Services` (`IBookService`/`BookService`) → EF Core (`ApplicationDbContext`) → SQLite.

## Unit testing guidelines (`Tests/UnitTests`)

- Target business logic in `BookService` (and model validation in `Book`), not controllers — controllers are thin and delegate.
- Structure every test as **Arrange → Act → Assert**, with clear separation between the three.
- Keep tests independent: no shared mutable state between tests, no reliance on test execution order. Each test class opens its own SQLite `:memory:` connection via `SqliteInMemoryTestBase` (see that class) — follow this existing pattern for new test classes rather than introducing mocks.
- Avoid unnecessary mocking. `BookService` takes a real `ApplicationDbContext` backed by an in-memory SQLite connection; don't mock the `DbContext` or introduce a repository abstraction just to make mocking possible.
- Prefer meaningful behavioral assertions (e.g. "status changed and `UpdatedAt` was stamped," "search returns only matching titles case-insensitively") over asserting incidental implementation details (e.g. exact SQL generated, internal method call counts).

## Integration testing guidelines (`Tests/IntegrationTests`)

- Use `WebApplicationFactory<Program>` (see `ReadingTrackerWebApplicationFactory`) to exercise the full HTTP → Controller → Service → EF Core → SQLite path.
- Prefer testing through HTTP (`HttpClient` calls to controller routes) when the behavior under test spans the request pipeline — routing, model binding, `ModelState` validation, redirects/views.
- Every test method gets a fresh, isolated SQLite database file — implement `IAsyncLifetime` (not `IClassFixture`) on the test class, run `Database.MigrateAsync()` on init, and in teardown call `SqliteConnection.ClearAllPools()` before deleting the temp DB file (otherwise the file stays locked). Never point tests at the dev database (`readingtracker.db`).
- Seed only the data a given test actually needs — don't reuse a large shared fixture "just in case."
- Where relevant, verify both the HTTP-level outcome (status code, redirect, rendered content) and that the database was actually persisted/updated correctly — a 200/302 response alone doesn't prove the write happened.

## Anti-forgery testing

- Never remove or weaken `[ValidateAntiForgeryToken]` on controller actions — it must stay unchanged in production code.
- In the test `WebApplicationFactory`, a no-op `IAntiforgery` may be substituted (as already done in `ReadingTrackerWebApplicationFactory`) so integration tests can POST forms without scraping tokens — but only when the test's purpose is controller/service/EF Core integration, not the antiforgery mechanism itself. If you're specifically testing anti-forgery behavior, don't use the substituted no-op.

## Test quality

- Tests must be independent and pass in any order or in isolation — no test-order dependencies.
- Avoid asserting implementation details that aren't part of the observable behavior/contract.
- A good test fails when the behavior it protects is broken, and only then — it shouldn't fail due to unrelated refactors.

## Validation

After writing or changing tests:

1. Run `dotnet build` — confirm the solution compiles.
2. Run `dotnet test` — confirm unit and integration tests pass.
3. If a test fails, investigate the root cause (bug in the code under test, wrong assertion, bad setup/isolation) and fix that — do not weaken or delete the test to make it pass.
