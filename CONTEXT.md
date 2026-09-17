# CONTEXT.md

Background and constraints for the Personal Reading Tracker. See `SPEC.MD` for the full original spec —
this file summarizes the parts that shape day-to-day decisions.

## Purpose

A single-user web app for tracking books across three statuses (Want to Read / Currently Reading / Completed),
with rating, notes, and title/author search.

**The application itself is secondary.** Per `SPEC.MD` §1 and §23, this project exists primarily as a hands-on
learning vehicle for automated testing (unit + integration, via xUnit) and CI/DevOps (GitHub Actions). The app
is kept intentionally simple so there's a realistic but small codebase to practice writing and maintaining tests
and a CI pipeline against — see `PLAN.md` for what's built vs. planned toward that goal.

## Technology stack (current)

| Area | Technology |
|---|---|
| Language | C# |
| Framework | ASP.NET Core MVC |
| .NET | .NET 10 |
| Database | SQLite |
| ORM | Entity Framework Core |
| Frontend | Razor Views, HTML, CSS (no JS framework) |
| Automated Testing | xUnit |
| Source Control | Git / GitHub |
| CI | GitHub Actions — implemented (`.github/workflows/ci.yml`), runs on push/PR to `main` |
| Authentication | None |

## Constraints and scope boundaries

Per `SPEC.MD` §20, the following are intentionally out of scope for this project (not oversights):

- User authentication, multiple user accounts
- Cloud database, production deployment
- Book cover images, external book APIs, ISBN lookup
- Social features, recommendations, reading statistics
- Mobile application, notifications
- Advanced/JS frontend frameworks

These constraints exist so the project doesn't distract from its actual goal (testing/CI practice); don't
propose adding them without the user explicitly asking first.

## Current facts vs. future plans

- **Implemented now**: Book CRUD, status tracking, search, unit tests, integration tests, GitHub Actions CI workflow (see `PLAN.md` for the phase breakdown).
- **Planned, not yet implemented**: an intentional CI-failure demonstration exercise (`SPEC.MD` §21 Phase 9).
- **Known open issues** (not yet fixed): tracked informally in the local, untracked `TODO.md` from a prior review pass — see `PLAN.md`'s "Current phase" section for a summary.
