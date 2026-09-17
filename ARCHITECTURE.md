# ARCHITECTURE.md

System design for the Personal Reading Tracker. See `CLAUDE.md` for the enforced architectural rules
(no repositories-over-EF-Core, no CQRS/MediatR, etc.) — this file describes what's actually built.

## Layered flow

```
User → BooksController → IBookService/BookService → ApplicationDbContext (EF Core) → SQLite
                                                              ↓
User ← Razor View        ← BooksController        ← (query result)
```

Controllers never touch `ApplicationDbContext` directly; all data access goes through `BookService`.

## Project structure

```
Controllers/   BooksController, HomeController
Models/        Book, ReadingStatus, ReadingStatusExtensions, ErrorViewModel
Data/          ApplicationDbContext (one DbSet<Book>, no OnModelCreating)
Services/      IBookService / BookService
Migrations/    One migration: InitialCreate (Books table)
Views/         Books/{Index,Create,Edit}, Home/{Index,Privacy}, Shared/{_Layout,Error,...}
Tests/         UnitTests, IntegrationTests
Program.cs     Composition root
```

There is no `Books/Delete.cshtml` — `Delete`, `StartReading`, and `Complete` are POST-only actions triggered
from buttons on `Index.cshtml`, with no dedicated view of their own.

## Composition root (`Program.cs`)

- `AddControllersWithViews()` — MVC with Razor views, no Razor Pages/API-only setup.
- `AddDbContext<ApplicationDbContext>(options => options.UseSqlite(...))` — SQLite via the connection string in `appsettings.json`.
- `AddScoped<IBookService, BookService>()` — the only application service registered.
- Middleware pipeline: exception handler + HSTS (non-dev only) → HTTPS redirection → routing → authorization → static assets → default convention route (`{controller=Home}/{action=Index}/{id?}`). No `UseAuthentication()` — there is no auth.
- Ends with `public partial class Program { }`, required so `WebApplicationFactory<Program>` can bootstrap the app in integration tests (top-level-statement `Program` is otherwise `internal`).

## Database approach

- Single `Books` table, matching `Book.cs` field-for-field (see the `InitialCreate` migration).
- Validation is expressed entirely through data annotations on `Book` (`[Required]`, `[StringLength]`, `[Range]`) — there is no Fluent API configuration (`OnModelCreating` is not overridden anywhere).
- `ReadingStatus` is a plain enum persisted as an `int`; reordering its members would silently change the meaning of existing rows (see the comment in `Models/ReadingStatus.cs`).
- Schema changes go through EF Core migrations (`dotnet ef migrations add`), not manual SQL.

## Key architectural decisions

- **Status changes and edits mutate the existing row in place** — `ChangeStatusAsync` and `UpdateBookAsync` load the row via `FindAsync`, update fields, and stamp `UpdatedAt`; they never insert a new row for what is logically the same book.
- **Search is pushed into SQLite**, not done in memory — `SearchBooksAsync` uses `EF.Functions.Like` on `Title`/`Author` so case-insensitive matching happens at the database layer.
- **Not-found is a return value, not an exception** — `BookService` methods return `bool` (`Update/Delete/ChangeStatusAsync`) or nullable (`GetBookAsync`) rather than throwing, so controllers branch on the result (`Edit` checks it and returns `NotFound()`; see `PLAN.md`/`TODO.md` for the current gaps in `Delete`/`StartReading`/`Complete`).

## Integration-test architecture

Integration tests run the real ASP.NET Core pipeline via `WebApplicationFactory<Program>`, with two things
substituted per test run: the `DbContext` (pointed at an isolated temp-file SQLite database instead of the dev
database) and `IAntiforgery` (replaced with a no-op so POSTs don't need scraped tokens). See `TESTING.md` for
the full mechanics (isolation, migrations, teardown) and `.claude/skills/dotnet-testing/SKILL.md` for the testing
guidelines that govern how these are used.
