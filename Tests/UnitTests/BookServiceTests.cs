using ReadingTracker.Models;
using ReadingTracker.Services;

namespace ReadingTracker.UnitTests;

public class BookServiceTests : SqliteInMemoryTestBase
{
    private readonly BookService _sut;

    public BookServiceTests()
    {
        _sut = new BookService(Context);
    }

    private static Book MakeBook(
        string title = "Clean Code",
        string author = "Robert C. Martin",
        string genre = "Technology",
        ReadingStatus status = ReadingStatus.WantToRead,
        int? rating = null,
        string? notes = null) => new()
        {
            Title = title,
            Author = author,
            Genre = genre,
            Status = status,
            Rating = rating,
            ReadingNotes = notes
        };

    [Fact]
    public async Task CreateBookAsync_WithValidBook_PersistsBookWithTimestamp()
    {
        var book = MakeBook();

        var created = await _sut.CreateBookAsync(book);

        Assert.NotEqual(0, created.Id);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Null(created.UpdatedAt);

        var stored = await _sut.GetBookAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal("Clean Code", stored!.Title);
    }

    [Fact]
    public async Task GetBooksAsync_ReturnsAllBooksOrderedByTitle()
    {
        await _sut.CreateBookAsync(MakeBook(title: "Zen and the Art of Motorcycle Maintenance"));
        await _sut.CreateBookAsync(MakeBook(title: "Atomic Habits"));
        await _sut.CreateBookAsync(MakeBook(title: "Mistborn"));

        var books = await _sut.GetBooksAsync();

        Assert.Equal(
            new[] { "Atomic Habits", "Mistborn", "Zen and the Art of Motorcycle Maintenance" },
            books.Select(b => b.Title));
    }

    [Fact]
    public async Task ChangeStatusAsync_WantToReadToCurrentlyReading_UpdatesExistingRecordInPlace()
    {
        var created = await _sut.CreateBookAsync(MakeBook(status: ReadingStatus.WantToRead));

        var result = await _sut.ChangeStatusAsync(created.Id, ReadingStatus.CurrentlyReading);

        Assert.True(result);

        var allBooks = await _sut.GetBooksAsync();
        var updated = Assert.Single(allBooks);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(ReadingStatus.CurrentlyReading, updated.Status);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task ChangeStatusAsync_NonExistentBook_ReturnsFalse()
    {
        var result = await _sut.ChangeStatusAsync(999, ReadingStatus.Completed);

        Assert.False(result);
    }

    [Fact]
    public async Task SearchBooksAsync_ByAuthor_ReturnsOnlyMatchingBooksCaseInsensitive()
    {
        await _sut.CreateBookAsync(MakeBook(title: "Clean Code", author: "Robert C. Martin"));
        await _sut.CreateBookAsync(MakeBook(title: "Dune", author: "Frank Herbert"));

        var results = await _sut.SearchBooksAsync("martin");

        var result = Assert.Single(results);
        Assert.Equal("Clean Code", result.Title);
    }

    [Fact]
    public async Task SearchBooksAsync_NoMatch_ReturnsEmptyList()
    {
        await _sut.CreateBookAsync(MakeBook(title: "Dune", author: "Frank Herbert"));

        var results = await _sut.SearchBooksAsync("nonexistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchBooksAsync_EmptyOrNullTerm_ReturnsAllBooks()
    {
        await _sut.CreateBookAsync(MakeBook(title: "Dune", author: "Frank Herbert"));
        await _sut.CreateBookAsync(MakeBook(title: "Mistborn", author: "Brandon Sanderson"));

        var resultsForNull = await _sut.SearchBooksAsync(null);
        var resultsForEmpty = await _sut.SearchBooksAsync("   ");

        Assert.Equal(2, resultsForNull.Count);
        Assert.Equal(2, resultsForEmpty.Count);
    }

    [Fact]
    public async Task UpdateBookAsync_ExistingBook_UpdatesFieldsAndTimestamp()
    {
        var created = await _sut.CreateBookAsync(MakeBook());

        created.Title = "Clean Code: Updated";
        created.Rating = 5;
        created.ReadingNotes = "Great read.";
        var result = await _sut.UpdateBookAsync(created);

        Assert.True(result);

        var stored = await _sut.GetBookAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal("Clean Code: Updated", stored!.Title);
        Assert.Equal(5, stored.Rating);
        Assert.Equal("Great read.", stored.ReadingNotes);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task UpdateBookAsync_NonExistentBook_ReturnsFalse()
    {
        var book = MakeBook();
        book.Id = 999;

        var result = await _sut.UpdateBookAsync(book);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteBookAsync_ExistingBook_RemovesBook()
    {
        var created = await _sut.CreateBookAsync(MakeBook());

        var result = await _sut.DeleteBookAsync(created.Id);

        Assert.True(result);
        Assert.Null(await _sut.GetBookAsync(created.Id));
    }

    [Fact]
    public async Task DeleteBookAsync_NonExistentBook_ReturnsFalse()
    {
        var result = await _sut.DeleteBookAsync(999);

        Assert.False(result);
    }
}
