using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReadingTracker.Data;

namespace ReadingTracker.IntegrationTests;

public class ReadingTrackerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"readingtracker-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Redirect the app under test away from the real dev database (readingtracker.db)
            // to a throwaway SQLite file unique to this test run, so tests can never affect it.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));

            // The controller requires an antiforgery token on every POST. Tests bypass that
            // check entirely rather than replicating the browser's token/cookie handshake,
            // since antiforgery validation isn't what these tests are meant to verify.
            services.RemoveAll<IAntiforgery>();
            services.AddSingleton<IAntiforgery, DisabledAntiforgery>();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        // Microsoft.Data.Sqlite pools connections by connection string, which keeps the
        // file handle open after the DbContext/factory are disposed. Clear the pool first
        // so the temp file can actually be deleted.
        SqliteConnection.ClearAllPools();

        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            File.Delete(_dbPath + suffix);
        }
    }

    private sealed class DisabledAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) =>
            new("ignore", "ignore", "ignore", "ignore");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) =>
            new("ignore", "ignore", "ignore", "ignore");

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void SetCookieTokenAndHeader(HttpContext httpContext) { }

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}
