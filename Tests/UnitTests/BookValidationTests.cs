using System.ComponentModel.DataAnnotations;
using ReadingTracker.Models;

namespace ReadingTracker.UnitTests;

public class BookValidationTests
{
    private static Book MakeValidBook() => new()
    {
        Title = "Clean Code",
        Author = "Robert C. Martin",
        Genre = "Technology",
        Status = ReadingStatus.WantToRead
    };

    private static bool TryValidate(Book book, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(book);
        return Validator.TryValidateObject(book, context, results, validateAllProperties: true);
    }

    [Fact]
    public void ValidBook_PassesValidation()
    {
        var isValid = TryValidate(MakeValidBook(), out var results);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void EmptyTitle_FailsValidation()
    {
        var book = MakeValidBook();
        book.Title = string.Empty;

        var isValid = TryValidate(book, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Title)));
    }

    [Fact]
    public void EmptyAuthor_FailsValidation()
    {
        var book = MakeValidBook();
        book.Author = string.Empty;

        var isValid = TryValidate(book, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Author)));
    }

    [Fact]
    public void EmptyGenre_FailsValidation()
    {
        var book = MakeValidBook();
        book.Genre = string.Empty;

        var isValid = TryValidate(book, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Genre)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rating_OutOfRange_FailsValidation(int rating)
    {
        var book = MakeValidBook();
        book.Rating = rating;

        var isValid = TryValidate(book, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Rating)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Rating_Boundary_PassesValidation(int rating)
    {
        var book = MakeValidBook();
        book.Rating = rating;

        var isValid = TryValidate(book, out var results);

        Assert.True(isValid);
    }

    [Fact]
    public void Rating_Null_PassesValidation()
    {
        var book = MakeValidBook();
        book.Rating = null;

        var isValid = TryValidate(book, out _);

        Assert.True(isValid);
    }

    [Fact]
    public void ReadingNotes_ExceedsMaxLength_FailsValidation()
    {
        var book = MakeValidBook();
        book.ReadingNotes = new string('a', 1001);

        var isValid = TryValidate(book, out var results);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.ReadingNotes)));
    }

    [Fact]
    public void ReadingNotes_AtMaxLength_PassesValidation()
    {
        var book = MakeValidBook();
        book.ReadingNotes = new string('a', 1000);

        var isValid = TryValidate(book, out _);

        Assert.True(isValid);
    }
}
