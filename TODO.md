# TODO

Findings from a `/code-review` pass on the test-project + docs work, kept here to revisit later. Nothing here has been fixed yet.

## Correctness

- **`Controllers/BooksController.cs:27`** — `Create(Book book)` binds the whole `Book` model, including `Id`, and `BookService.CreateBookAsync` never resets it before inserting. A POST with a guessable existing `Id` can collide with an existing row (unhandled `DbUpdateException`) or squat on a future autoincrement value.
- **`Services/BookService.cs:28`** — `SearchBooksAsync` builds its SQL `LIKE` pattern from the raw search term without escaping `%`/`_`. Searching for something literally containing `%` or `_` (e.g. "50% Off") gets misinterpreted as a wildcard instead of matched literally.
- **`Controllers/BooksController.cs:58`** — `Delete`, `StartReading`, and `Complete` all ignore the `bool` returned by `IBookService`, unlike `Edit` which checks it and returns `NotFound`. Acting on a nonexistent/already-deleted book id silently redirects as if it succeeded.

## Test reliability

- **`Tests/IntegrationTests/ReadingTrackerWebApplicationFactory.cs:50`** — `DisposeAsync` calls the process-wide `SqliteConnection.ClearAllPools()`. Fine with a single test class today, but once a second integration test class exists, parallel test execution could race between one test's teardown and another's still-open connection.

## Spec compliance

- **`Views/Books/Index.cshtml:54`** — Ratings render as N filled stars only, not the filled+empty 5-star format `SPEC.MD` §4.4 specifies (e.g. `★★★☆☆` for a 3-star book).

## Simplification

- **`Views/Books/Create.cshtml:5`** — The genre dropdown list is duplicated verbatim in `Create.cshtml` and `Edit.cshtml`. Should live in one shared place so they can't drift apart.
