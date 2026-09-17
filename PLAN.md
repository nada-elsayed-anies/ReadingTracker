# PLAN.md

Phase-level status for the Personal Reading Tracker, tracked against `SPEC.MD` §21's implementation phases.
This is not a task tracker — see `TODO.md` (local, untracked) for granular open issues.

## Current status

The application's core functionality and both test projects are implemented. CI is not yet set up.

## Completed phases

- **Phase 1 — Project Setup**: ASP.NET Core MVC project, SQLite + EF Core configured.
- **Phase 2 — Basic Book CRUD**: Create/Read/Update/Delete via `BooksController` → `BookService`.
- **Phase 3 — Reading Status**: `ReadingStatus` enum, quick-action status changes (`StartReading`/`Complete`) that update the existing row in place.
- **Phase 4 — Search**: title/author search via `SearchBooksAsync`.
- **Phase 5 — UI Improvements**: basic Razor views and CSS for the book list/forms.
- **Phase 6 — Unit Tests**: `Tests/UnitTests` covers `BookService` and `Book` validation.
- **Phase 7 — Integration Tests**: `Tests/IntegrationTests` covers the full HTTP → Controller → Service → EF Core → SQLite path.

## Current phase

No feature work is actively in progress. A prior `/code-review` pass left unresolved findings in `TODO.md`
(untracked, not yet committed) — known issues, not scheduled work:

- Mass-assignment risk: `Create(Book book)` binds `Id` directly.
- `SearchBooksAsync`'s `LIKE` pattern doesn't escape literal `%`/`_` in search terms.
- `Delete`/`StartReading`/`Complete` ignore the service's `bool` result (unlike `Edit`).
- `ClearAllPools()` in the integration test factory teardown is process-wide, a possible race if a second integration test class is added.
- Star-rating rendering doesn't yet match `SPEC.MD` §4.4's filled+empty format.
- The genre dropdown is duplicated across `Create.cshtml`/`Edit.cshtml`.

## Next planned phases

- **Phase 8 — CI**: add a GitHub Actions workflow per `SPEC.MD` §15–17 (restore → build → unit tests → integration tests, triggered on push/PR to `main`, failing on any error).
- **Phase 9 — CI Failure Demonstration**: intentionally break a test to observe CI catch it, then fix it and confirm the pipeline goes green — per `SPEC.MD` §21, this is part of the project's learning objective, not a bug to avoid.
