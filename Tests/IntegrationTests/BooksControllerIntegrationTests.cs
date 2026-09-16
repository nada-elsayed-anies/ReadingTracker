using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReadingTracker.Data;
using ReadingTracker.Models;

namespace ReadingTracker.IntegrationTests;

// Implementing IAsyncLifetime here (rather than a shared IClassFixture) means xUnit creates
// a fresh factory - and therefore a fresh database - for every single test method below,
// so tests never see leftover data from each other.
public class BooksControllerIntegrationTests : IAsyncLifetime
{
    private ReadingTrackerWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ReadingTrackerWebApplicationFactory();
        await ((IAsyncLifetime)_factory).InitializeAsync();
        // AllowAutoRedirect = false so the client returns the controller's redirect response
        // itself (to assert on it) instead of silently following it to the next page.
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await ((IAsyncLifetime)_factory).DisposeAsync();
    }

    private async Task<Book> SeedBookAsync(Book book)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return book;
    }

    private async Task<Book?> FindBookAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Books.FindAsync(id);
    }

    [Fact]
    public async Task CreateBook_PersistsToDatabase()
    {
        var form = new Dictionary<string, string>
        {
            ["Title"] = "Clean Code",
            ["Author"] = "Robert C. Martin",
            ["Genre"] = "Technology",
            ["Status"] = ((int)ReadingStatus.WantToRead).ToString()
        };

        var response = await _client.PostAsync("/Books/Create", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await db.Books.SingleAsync(b => b.Title == "Clean Code");
        Assert.Equal("Robert C. Martin", stored.Author);
        Assert.Equal("Technology", stored.Genre);
        Assert.Equal(ReadingStatus.WantToRead, stored.Status);
    }

    [Fact]
    public async Task Index_ReturnsExpectedBooks()
    {
        await SeedBookAsync(new Book { Title = "Dune", Author = "Frank Herbert", Genre = "Sci-Fi" });

        var response = await _client.GetAsync("/Books");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Dune", body);
    }

    [Fact]
    public async Task Complete_ChangesStatusInDatabase()
    {
        var book = await SeedBookAsync(new Book
        {
            Title = "Mistborn",
            Author = "Brandon Sanderson",
            Genre = "Fantasy",
            Status = ReadingStatus.WantToRead
        });

        var response = await _client.PostAsync($"/Books/Complete/{book.Id}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var updated = await FindBookAsync(book.Id);
        Assert.NotNull(updated);
        Assert.Equal(ReadingStatus.Completed, updated!.Status);
    }

    [Fact]
    public async Task DeleteBook_RemovesFromDatabase()
    {
        var book = await SeedBookAsync(new Book
        {
            Title = "Atomic Habits",
            Author = "James Clear",
            Genre = "Self-Help"
        });

        var response = await _client.PostAsync($"/Books/Delete/{book.Id}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(await FindBookAsync(book.Id));
    }
}
