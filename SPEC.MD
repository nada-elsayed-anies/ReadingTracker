# Personal Reading Tracker

## Technical Specification

### 1. Project Overview

**Personal Reading Tracker** is a simple web application that allows a user to manage and track books they want to read, are currently reading, or have completed.

The application is primarily being developed as a **learning project for CI pipelines and automated testing**. The application itself should therefore remain simple while providing enough functionality to create meaningful automated tests.

### 2. Technology Stack

| Area              | Technology             |
| ----------------- | ---------------------- |
| Language          | C#                     |
| Framework         | ASP.NET Core MVC       |
| .NET              | Current LTS version    |
| Database          | SQLite                 |
| ORM               | Entity Framework Core  |
| Frontend          | Razor Views, HTML, CSS |
| Automated Testing | xUnit                  |
| Source Control    | Git                    |
| Repository        | GitHub                 |
| CI                | GitHub Actions         |
| Authentication    | None                   |

No JavaScript framework or frontend framework is required.

---

# 3. Functional Requirements

## 3.1 Book Management

The user must be able to:

* Add a new book.
* View all books.
* Edit an existing book.
* Delete a book.
* Change a book's reading status.

Each book contains:

* Title
* Author
* Genre
* Reading Status
* Rating
* Reading Notes

---

## 3.2 Reading Status

Every book must have exactly one of the following statuses:

1. **Want to Read**
2. **Currently Reading**
3. **Completed**

Books should be displayed grouped by their status.

Example:

```text
Want to Read
    Book A
    Book B

Currently Reading
    Book C

Completed
    Book D
    Book E
```

### Quick Actions

Books should have quick-action buttons depending on their current status.

For example:

**Want to Read**

```text
[Start Reading] [Delete]
```

**Currently Reading**

```text
[Complete] [Delete]
```

**Completed**

```text
[Delete]
```

A status change should update the existing database record rather than creating a new book.

---

# 4. Book Fields

## 4.1 Title

* Required.
* Cannot be empty.
* Should have a reasonable maximum length, e.g. 200 characters.

## 4.2 Author

* Required.
* Cannot be empty.
* Maximum length: 150 characters.

## 4.3 Genre

Genre should be selected from a dropdown.

Suggested initial values:

* Fiction
* Fantasy
* Mystery
* Thriller
* Romance
* Science Fiction
* Biography
* History
* Self-Development
* Technology
* Other

The list can be expanded later.

## 4.4 Rating

Rating is an integer from **1 to 5**.

Rating is optional.

The user does not have to provide a rating when adding a book.

The UI should display ratings as stars.

Example:

```text
★★★★★
★★★★☆
★★★☆☆
```

A rating is particularly useful for completed books, but the application does not need to enforce that ratings can only be entered for completed books.

## 4.5 Reading Notes

* Optional.
* Short free-text field.
* Maximum length: 1,000 characters.

Examples:

```text
Interesting characters and good pacing.
```

or

```text
Need to continue from chapter 5.
```

---

# 5. Search

The main page should contain a search bar.

The user can search by:

* Book title
* Author

Search should be:

* Case-insensitive.
* Applied across all three statuses.
* Updated when the user submits the search.

Example:

```text
Search: "martin"

Want to Read
    Clean Code — Robert C. Martin
```

If there are no matching books, display a simple message:

```text
No books found.
```

The search should not modify or delete the underlying data.

---

# 6. Database

SQLite will be used as the application's database.

Entity Framework Core will handle database access.

## Book Table

Suggested model:

```text
Book
--------------------------------
Id              INTEGER PK
Title           TEXT NOT NULL
Author          TEXT NOT NULL
Genre           TEXT NOT NULL
Status          INTEGER NOT NULL
Rating          INTEGER NULL
ReadingNotes    TEXT NULL
CreatedAt       DATETIME NOT NULL
UpdatedAt       DATETIME NULL
```

### Status Representation

The application should use a C# enum:

```text
WantToRead
CurrentlyReading
Completed
```

The enum will be stored as an integer in SQLite.

### Validation

Validation should exist at the application/model level.

Examples:

```text
Title       → Required
Author      → Required
Genre       → Required
Rating      → 1–5
Notes       → Maximum 1000 characters
```

Database migrations should be used to create and update the database schema.

---

# 7. Application Architecture

The project should use a simple layered structure without introducing unnecessary enterprise architecture.

Suggested structure:

```text
ReadingTracker/
│
├── Controllers/
│   └── BooksController.cs
│
├── Models/
│   ├── Book.cs
│   └── ReadingStatus.cs
│
├── Data/
│   └── ApplicationDbContext.cs
│
├── Services/
│   └── BookService.cs
│
├── Views/
│   ├── Books/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   └── Edit.cshtml
│   └── Shared/
│
├── wwwroot/
│   └── css/
│
├── Tests/
│   ├── UnitTests/
│   └── IntegrationTests/
│
└── Program.cs
```

The service layer is intentionally small.

Its purpose is also educational: it gives us business logic that can be tested independently from the controller and UI.

---

# 8. Main Application Flow

## View Books

```text
User
 ↓
BooksController
 ↓
BookService
 ↓
Entity Framework Core
 ↓
SQLite
 ↓
BooksController
 ↓
Razor View
 ↓
User
```

## Add Book

```text
User submits form
       ↓
BooksController
       ↓
Model Validation
       ↓
BookService
       ↓
EF Core
       ↓
SQLite
       ↓
Redirect to Book List
```

## Change Status

Example:

```text
[Complete]
    ↓
BooksController
    ↓
BookService
    ↓
Find Book
    ↓
Set Status = Completed
    ↓
Save Changes
    ↓
Redirect
```

---

# 9. Controllers

The main controller will be:

```text
BooksController
```

Suggested actions:

```text
Index()
Create()
Create(Book book)
Edit(int id)
Edit(int id, Book book)
Delete(int id)
StartReading(int id)
Complete(int id)
```

The controller should remain thin.

Business logic should preferably be handled by `BookService`.

---

# 10. Service Layer

`BookService` should contain operations such as:

```text
GetBooks()
SearchBooks(searchTerm)
GetBook(id)
CreateBook(book)
UpdateBook(book)
DeleteBook(id)
ChangeStatus(id, status)
```

This gives us a useful place to write unit tests.

For example:

```text
ChangeStatus(bookId, Completed)
```

can be tested without requiring the browser.

---

# 11. UI Requirements

The main page should contain:

```text
Personal Reading Tracker

[ Search by title or author... ] [Search]

---------------------------------

Want to Read

[Book Card]
Title
Author
Genre

[Start Reading] [Edit] [Delete]

---------------------------------

Currently Reading

[Book Card]
Title
Author
Genre

[Complete] [Edit] [Delete]

---------------------------------

Completed

[Book Card]
Title
Author
Genre
★★★★★
Notes

[Edit] [Delete]

---------------------------------

[ + Add Book ]
```

The design should be clean and simple.

The UI does not need animations, advanced JavaScript, authentication, user profiles, or other unnecessary features.

---

# 12. Automated Testing Strategy

Automated testing is one of the main objectives of the project.

Testing will be divided into two levels.

## 12.1 Unit Tests

Unit tests should test business logic independently.

Examples:

### Book creation

```text
Given valid book information
When CreateBook is called
Then a book should be created.
```

### Validation

```text
Given an empty title
When the book is validated
Then validation should fail.
```

### Rating

```text
Given a rating of 6
When the book is validated
Then validation should fail.
```

### Status

```text
Given a WantToRead book
When ChangeStatus is called with CurrentlyReading
Then the book status should become CurrentlyReading.
```

### Search

```text
Given books by different authors
When searching for an author
Then only matching books should be returned.
```

### Delete

```text
Given an existing book
When DeleteBook is called
Then the book should no longer exist.
```

---

# 13. Integration Tests

Integration tests will verify that multiple application components work together.

Examples:

### Create Book

```text
HTTP POST
   ↓
Controller
   ↓
Service
   ↓
EF Core
   ↓
Database
```

Test that a book submitted through the application is actually stored.

### Retrieve Books

Test that:

```text
Database
   ↓
EF Core
   ↓
Controller
   ↓
HTTP Response
```

returns the expected books.

### Change Status

Test that clicking/triggering the completion action actually changes the database status.

### Delete Book

Test that deleting a book through the application removes it from the database.

---

# 14. Test Database

Tests should not depend on the developer's normal application database.

Integration tests should use a separate test database/environment.

The goal is:

```text
Development Database
        ≠
Test Database
```

This prevents tests from modifying real development data.

---

# 15. CI Pipeline

GitHub Actions will automatically execute the application's verification process whenever code is pushed or a Pull Request is created.

Initial pipeline:

```text
Developer
    ↓
git push
    ↓
GitHub
    ↓
GitHub Actions
    ↓
Checkout code
    ↓
Install .NET
    ↓
Restore dependencies
    ↓
Build application
    ↓
Run automated tests
    ↓
Publish test results
    ↓
PASS / FAIL
```

---

# 16. CI Pipeline Requirements

The pipeline should:

1. Trigger on pushes to the main branch.
2. Trigger on Pull Requests targeting the main branch.
3. Checkout the repository.
4. Install the required .NET SDK.
5. Restore NuGet dependencies.
6. Build the solution.
7. Run unit tests.
8. Run integration tests.
9. Fail if any test fails.
10. Fail if the application cannot be built.

Example:

```text
Restore
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
```

Every stage must succeed before the pipeline is considered successful.

---

# 17. CI Failure Behavior

A failed pipeline should prevent the change from being considered successfully verified.

For example:

```text
Developer pushes code
        ↓
Build ✓
        ↓
Unit Tests ✓
        ↓
Integration Tests ✗
        ↓
Pipeline FAILED
```

GitHub Actions should clearly show which step failed and provide the test/build output.

This is an important part of the learning objective.

The project should intentionally include situations where we can break a test and observe how CI detects the problem.

---

# 18. Branch and Pull Request Workflow

The recommended workflow is:

```text
main
 │
 ├── feature/add-book
 │
 ├── feature/search
 │
 └── feature/testing
```

Development workflow:

```text
Create branch
     ↓
Implement feature
     ↓
Write/update tests
     ↓
Push branch
     ↓
Create Pull Request
     ↓
GitHub Actions runs
     ↓
Build + Tests
     ↓
Review
     ↓
Merge into main
```

This will allow the project to demonstrate the practical value of CI rather than simply having a workflow file that runs after every push.

---

# 19. Non-Functional Requirements

## Simplicity

The application should remain small and understandable.

## Maintainability

Code should be separated into:

```text
Controller
Service
Data Access
Model
```

## Testability

Business logic should be designed so it can be tested without requiring the UI.

## Reliability

The CI pipeline must detect:

* Build failures.
* Failed unit tests.
* Failed integration tests.

## Usability

The user should be able to add, search, update, and delete books without unnecessary steps.

---

# 20. Out of Scope

The following features are intentionally excluded from the initial version:

* User authentication.
* Multiple user accounts.
* Cloud database.
* Book cover images.
* External book APIs.
* ISBN lookup.
* Social features.
* Recommendations.
* Reading statistics.
* Mobile application.
* Notifications.
* Advanced frontend frameworks.
* Production deployment.

These can be added later if needed, but they should not distract from the CI/testing objective.

---

# 21. Implementation Phases

The project should be built incrementally.

### Phase 1 — Project Setup

* Create ASP.NET Core MVC project.
* Configure Git.
* Create GitHub repository.
* Configure SQLite.
* Configure EF Core.

### Phase 2 — Basic Book CRUD

Implement:

```text
Create
Read
Update
Delete
```

### Phase 3 — Reading Status

Add:

```text
Want to Read
Currently Reading
Completed
```

and quick status actions.

### Phase 4 — Search

Implement title/author filtering.

### Phase 5 — UI Improvements

Add CSS and improve the book cards and layout.

### Phase 6 — Unit Tests

Create the xUnit test project and test the service/business logic.

### Phase 7 — Integration Tests

Test important application flows using the test database.

### Phase 8 — CI

Create the GitHub Actions workflow.

Verify that:

```text
Build ✓
Unit Tests ✓
Integration Tests ✓
```

are automatically executed.

### Phase 9 — CI Failure Demonstration

Intentionally introduce a failing test and observe:

```text
Code pushed
     ↓
CI starts
     ↓
Test fails
     ↓
Pipeline fails
```

Then fix the problem and verify:

```text
Pipeline passes ✓
```

---

# 22. Definition of Done

The initial version is considered complete when:

* [ ] A user can add a book.
* [ ] A user can edit a book.
* [ ] A user can delete a book.
* [ ] Books are grouped by reading status.
* [ ] A user can move a book between statuses.
* [ ] A user can search by title or author.
* [ ] Genre can be selected from a dropdown.
* [ ] Rating supports 1–5 stars.
* [ ] Reading notes can be stored.
* [ ] Data is persisted in SQLite.
* [ ] Input validation works.
* [ ] Unit tests exist for core business logic.
* [ ] Integration tests exist for important application flows.
* [ ] All tests pass locally.
* [ ] GitHub Actions automatically builds the application.
* [ ] GitHub Actions automatically runs the tests.
* [ ] The pipeline fails when a test fails.
* [ ] The pipeline passes again after the problem is fixed.

---

# 23. Main Learning Objective

The application is **not the primary objective** of this project.

The application provides a realistic but simple codebase on which to learn:

```text
Application Development
        ↓
Automated Testing
        ↓
Continuous Integration
        ↓
Feedback from CI
        ↓
Fix → Push → Test Again
```

By the end of the project, the developer should understand not only **how to create a GitHub Actions CI pipeline**, but also **why automated tests are valuable and how CI uses them to automatically verify changes**.
