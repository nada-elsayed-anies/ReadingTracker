# CONVENTIONS.md

Coding conventions actually followed in this codebase. See `CLAUDE.md` for the enforced architectural
boundaries (layering, what not to introduce) — this file covers code-level style and patterns.

## Naming

- PascalCase for classes, methods, and properties (standard .NET convention).
- Async methods are suffixed `Async` (`GetBooksAsync`, `ChangeStatusAsync`, etc.) and return `Task`/`Task<T>`.
- Interfaces prefixed with `I` (`IBookService`).
- Nullable reference types are enabled project-wide (`<Nullable>enable</Nullable>`) — nullability is expressed in the type (`Book?`, `string?`), not through comments or conventions.

## Async/await

- Anything that touches `ApplicationDbContext` is `async Task`/`async Task<T>` — every `BookService` method and every `BooksController` action that calls into it.
- `HomeController` is fully synchronous (`Index`, `Privacy`, `Error` all return `IActionResult` directly) because it does no I/O — this is deliberate, not an inconsistency. Don't make a controller action `async` unless it actually awaits something.

## Controller / service responsibilities

- Controllers are thin: check `ModelState.IsValid`, delegate to `IBookService`, and return a view/redirect/`NotFound()`. They never construct EF Core queries or touch `ApplicationDbContext` directly.
- All business logic and data access lives in `BookService`, which is what makes it unit-testable independent of HTTP (see `TESTING.md`).
- Controllers depend on `IBookService`, never the concrete `BookService`, via constructor injection.

## EF Core conventions

- Rely on EF Core's naming/mapping conventions (a `DbSet<Book> Books` maps to a `Books` table by property name) rather than Fluent API — there is no `OnModelCreating` override in this project. Keep it that way unless a mapping genuinely can't be expressed with data annotations.
- All validation constraints (`Required`, `StringLength`, `Range`) are expressed as data annotations directly on `Book`, not in a separate validator class.
- Schema changes go through `dotnet ef migrations add`, never hand-edited SQL or a changed model without a matching migration.
- Updates and status changes load the existing entity via `FindAsync` and mutate it in place (stamping `UpdatedAt`), rather than inserting a new row — preserve this pattern for any new mutating operation.

## Error handling

- No `try`/`catch` anywhere in `BookService` or the controllers. Expected "not found" cases are represented as return values — `bool` for `Update/Delete/ChangeStatusAsync`, nullable `Book?` for `GetBookAsync` — not exceptions.
- Controllers branch on these return values to decide between `NotFound()` and success (see `Edit` in `BooksController` for the pattern to follow — check the `TODO.md` note about `Delete`/`StartReading`/`Complete` currently not doing this consistently).

## Dependency injection

- Constructor injection only — no service locator, no static access to services.
- `IBookService` and `ApplicationDbContext` are registered as scoped (the ASP.NET Core default for a per-request `DbContext`).

## Keep it simple

Per `CLAUDE.md`, this project deliberately avoids enterprise patterns: no repository layer on top of EF Core,
no CQRS/MediatR, no additional abstraction layers. A new feature should extend `BookService` and the existing
controller pattern, not introduce a new architectural layer.
