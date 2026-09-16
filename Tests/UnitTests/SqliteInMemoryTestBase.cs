using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReadingTracker.Data;

namespace ReadingTracker.UnitTests;

public abstract class SqliteInMemoryTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly ApplicationDbContext Context;

    protected SqliteInMemoryTestBase()
    {
        // Uses the real SQLite provider (not EF Core's UseInMemoryDatabase) so tests behave
        // like production SQLite — constraints, LIKE, etc. all work the same way. A SQLite
        // in-memory database only exists as long as one connection to it stays open, which is
        // why the connection is opened here and kept alive for the whole test's lifetime.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
