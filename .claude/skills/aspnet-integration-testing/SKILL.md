---
name: aspnet-integration-testing
description: Concrete patterns for writing ASP.NET Core integration tests in this project — WebApplicationFactory setup, per-test SQLite isolation, HTTP request/assertion shape, and antiforgery handling
---

# ASP.NET Core Integration Testing — Personal Reading Tracker

## When to use this skill

Use this skill when writing or modifying a test in `Tests/IntegrationTests`, or when adding a new controller action that needs HTTP-level coverage. For the general policy on what belongs in a unit test vs. an integration test, see `dotnet-testing` — this skill is the "how, exactly" companion: it documents the concrete code shape already used by `ReadingTrackerWebApplicationFactory` and `BooksControllerIntegrationTests` so new tests match it instead of re-deriving the pattern.

## Test class structure

- One test class per controller, named `{Controller}IntegrationTests` (e.g. `BooksControllerIntegrationTests`), namespace `ReadingTracker.IntegrationTests`, file-scoped namespace declaration.
- `[Fact]`-based; method naming `{Action}_{ExpectedOutcome}` (e.g. `CreateBook_PersistsToDatabase`, `Complete_ChangesStatusInDatabase`).
- No shared base class or `IClassFixture` today — keep that pattern for new classes rather than introducing an abstract base or shared fixture.

## How `ReadingTrackerWebApplicationFactory` is used

- It both extends `WebApplicationFactory<Program>` **and** implements `IAsyncLifetime` on the same class.
- `ConfigureWebHost` does two `RemoveAll<T>()` + `Add...` swaps in `ConfigureServices`:
  - `DbContextOptions<ApplicationDbContext>` → SQLite pointed at a per-instance temp file.
  - `IAntiforgery` → a hand-written no-op singleton (`DisabledAntiforgery`).
- `IAsyncLifetime.DisposeAsync()` is implemented **explicitly** (`async Task IAsyncLifetime.DisposeAsync()`) so it can coexist with the base class's own `DisposeAsync()`. Because of this, calling code must cast: `await ((IAsyncLifetime)_factory).InitializeAsync();` / `await ((IAsyncLifetime)_factory).DisposeAsync();` — a plain `_factory.DisposeAsync()` call resolves to the base class method and skips the temp-file cleanup.

## Per-test SQLite isolation

- Each factory instance computes its own unique temp DB path in a field initializer: `Path.Combine(Path.GetTempPath(), $"readingtracker-test-{Guid.NewGuid():N}.db")`.
- The test class implements `IAsyncLifetime` directly (never `IClassFixture<ReadingTrackerWebApplicationFactory>`), so xUnit creates a fresh factory — and therefore a fresh database — per test method:
  ```csharp
  public async Task InitializeAsync()
  {
      _factory = new ReadingTrackerWebApplicationFactory();
      await ((IAsyncLifetime)_factory).InitializeAsync();
      _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
  }
  ```
- `InitializeAsync()` on the factory runs `Database.MigrateAsync()` (real EF Core migrations, not `EnsureCreated()`) against the temp file, so schema matches production.
- Teardown order matters: dispose the `HttpClient` first, then `await ((IAsyncLifetime)_factory).DisposeAsync()`, which internally does `await base.DisposeAsync()` → `SqliteConnection.ClearAllPools()` → delete the `.db`, `-shm`, and `-wal` files. The pool must be cleared *before* deleting, or the delete fails/throws on Windows because Microsoft.Data.Sqlite keeps a pooled connection (and file handle) open by connection string.

## Seeding test data

Seed and verify directly through `ApplicationDbContext`, opening a fresh short-lived `IServiceScope` per call — never via HTTP, except when the HTTP action under test *is* the create path itself:

```csharp
private async Task<Book> SeedBookAsync(Book book)
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Books.Add(book);
    await db.SaveChangesAsync();
    return book;
}
```

Use the same scope-per-call pattern for post-request verification (e.g. a `FindBookAsync(id)` helper, or an inline `db.Books.SingleAsync(...)` when there's no id yet, as in the Create test). Never hold a long-lived `DbContext`/scope across a test method.

## Constructing HTTP requests

- Create the client with `AllowAutoRedirect = false` so redirect responses can be asserted directly instead of followed:
  `_factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`.
- GET: `await _client.GetAsync("/Books")`, then `await response.Content.ReadAsStringAsync()` for content checks.
- POST: build a `Dictionary<string, string>` of field name → value and wrap it in `FormUrlEncodedContent` — no JSON, no raw `HttpRequestMessage`:
  ```csharp
  var form = new Dictionary<string, string>
  {
      ["Title"] = "Clean Code",
      ["Author"] = "Robert C. Martin",
      ["Genre"] = "Technology",
      ["Status"] = ((int)ReadingStatus.WantToRead).ToString()
  };
  var response = await _client.PostAsync("/Books/Create", new FormUrlEncodedContent(form));
  ```
- For id-only routes with no form fields (e.g. `Complete`, `Delete`), still pass an empty `FormUrlEncodedContent([])`:
  `await _client.PostAsync($"/Books/Complete/{book.Id}", new FormUrlEncodedContent([]));`

## What to assert

- **HTTP status**: `Assert.Equal(HttpStatusCode.Redirect, response.StatusCode)` for POST actions (valid because `AllowAutoRedirect = false` was set); `Assert.Equal(HttpStatusCode.OK, response.StatusCode)` for GETs.
- **Content**: plain substring checks on the raw HTML body, e.g. `Assert.Contains("Dune", body)` — no HTML parser library is used.
- **Database state**: always re-query via a fresh scope after the HTTP call and assert the actual persisted values — a status code alone doesn't prove the write happened. E.g. after POSTing to `Complete`, re-fetch the book and assert `Assert.Equal(ReadingStatus.Completed, updated!.Status)`; after `Delete`, assert the record is now `null`.

## Antiforgery handling

- `[ValidateAntiForgeryToken]` must stay unchanged in controllers — never remove or weaken it in production code to make a test easier.
- `ReadingTrackerWebApplicationFactory` substitutes a no-op `DisabledAntiforgery` (`IsRequestValidAsync`/`ValidateRequestAsync` always succeed, `SetCookieTokenAndHeader` no-ops), which is why tests never scrape a `__RequestVerificationToken` field or manage antiforgery cookies.
- This substitution is appropriate for tests whose purpose is controller/service/EF Core integration. If a test's whole purpose is verifying antiforgery *rejection* behavior itself, it must not rely on the standard factory's no-op — that scenario needs a dedicated setup that keeps real `IAntiforgery` wired in, which isn't what this factory provides.

## Common mistakes to avoid

- Pointing any test at `readingtracker.db` (the real dev database) instead of a per-test temp file.
- Sharing a `WebApplicationFactory`/database across tests via `IClassFixture` — breaks isolation and causes order-dependent failures.
- Asserting only a 200/302 status code without checking database state or response content.
- Mocking `ApplicationDbContext`/EF Core instead of exercising the real SQLite + migration path.
- Removing or weakening `[ValidateAntiForgeryToken]` in production code just to simplify a test.
- Forgetting `SqliteConnection.ClearAllPools()` before deleting the temp DB file (causes a locked-file error on Windows).

## Standard workflow

Inspect existing patterns → arrange an isolated DB (new `ReadingTrackerWebApplicationFactory` instance via the test class's `IAsyncLifetime.InitializeAsync`) → seed data (direct `ApplicationDbContext` in a fresh scope) → send the HTTP request (`FormUrlEncodedContent` / `GetAsync`) → assert the response (status code + content) → verify DB state (fresh scope, re-query) → cleanup (`IAsyncLifetime.DisposeAsync`) → run `dotnet test Tests/IntegrationTests/ReadingTracker.IntegrationTests.csproj`.
