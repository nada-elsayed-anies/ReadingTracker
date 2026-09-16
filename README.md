# Reading Tracker

A simple personal reading tracker web app. You can add books, mark them as
**Want to Read**, **Currently Reading**, or **Completed**, search your list, rate
finished books, and keep short reading notes.

This is a **learning project**. Its main purpose is to practice building an
ASP.NET Core MVC app together with automated tests and a Continuous
Integration (CI) pipeline — see `SPEC.MD` for the full technical spec.

## Technology stack

| Layer         | Technology                          |
|---------------|--------------------------------------|
| Framework     | ASP.NET Core MVC (.NET 10)           |
| Database      | SQLite                               |
| Data access   | Entity Framework Core                |
| Views         | Razor views                          |
| Testing       | xUnit                                |
| CI (planned)  | GitHub Actions                       |

## How to install and run

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Create the local SQLite database from the EF Core migrations:
   ```bash
   dotnet ef database update
   ```
   (This requires the `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`.)
4. Run the app:
   ```bash
   dotnet run
   ```
   or, for hot reload while developing:
   ```bash
   dotnet watch run
   ```
5. Open the app in your browser at `http://localhost:5292` (see
   `Properties/launchSettings.json` for the exact port).

## How the SQLite database is used

The app stores everything in a single SQLite file, `readingtracker.db`, in the
project root. The connection string lives in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=readingtracker.db"
}
```

The table schema (currently just a `Books` table) comes from the EF Core
migration in the `Migrations/` folder — `dotnet ef database update` applies
it. Each book's reading status (`WantToRead` / `CurrentlyReading` /
`Completed`) is stored as a plain integer, since that's how EF Core saves
C# enums by default.

`readingtracker.db` is listed in `.gitignore`, so it's never committed — each
person running the app gets their own local copy.

## Project structure

```
Controllers/            Thin MVC controllers (BooksController, HomeController) — delegate logic to Services
Models/                 Book, ReadingStatus (enum), ErrorViewModel — the data EF Core maps to the database
Data/                   ApplicationDbContext — the EF Core "gateway" to the SQLite database
Services/               BookService — where the actual business logic lives (create/search/update/delete books)
Migrations/             EF Core migrations that build/update the database schema
Views/                  Razor views (the HTML pages)
Tests/
  UnitTests/             Tests for BookService and Book validation, without any web server involved
  IntegrationTests/      Tests that make real HTTP requests through the whole app into a throwaway database
```

## Running tests locally

Run everything (both test projects):

```bash
dotnet test
```

Run just one test project:

```bash
dotnet test Tests/UnitTests/ReadingTracker.UnitTests.csproj
dotnet test Tests/IntegrationTests/ReadingTracker.IntegrationTests.csproj
```

Run a single test by name:

```bash
dotnet test --filter FullyQualifiedName~<TestName>
```

## What tests exist today

- **Unit tests** (`Tests/UnitTests`) — test `BookService` and the `Book`
  model's validation rules directly, in isolation, using a real (but
  temporary, in-memory) SQLite database. No web server or HTTP is involved,
  so these run fast and are the easiest place to check business logic like
  "does search match case-insensitively?" or "is Rating required to be
  between 1 and 5?".
- **Integration tests** (`Tests/IntegrationTests`) — start up the actual web
  application in memory and send it real HTTP requests (e.g. "POST a new
  book to `/Books/Create`"), then check the result actually landed in the
  database. These use their own temporary SQLite database file, so they
  never touch your real `readingtracker.db`.

## How tests are triggered/executed

Right now, tests are run manually with the `dotnet test` command shown
above. `dotnet test` looks at the solution (`ReadingTracker.slnx`), finds
both test projects, and runs every test method marked `[Fact]` or
`[Theory]` in them.

## Future CI pipeline

A GitHub Actions workflow will eventually be added so tests run
automatically instead of manually. The plan (see `SPEC.MD` §15–17) is:

1. Trigger on every push and pull request to `main`.
2. Restore dependencies and build the project.
3. Run the unit tests.
4. Run the integration tests.
5. Fail the whole pipeline if the build or any test fails.

The point of this is to catch broken code automatically before it gets
merged, instead of relying on someone remembering to run the tests by hand.
