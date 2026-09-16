using Microsoft.EntityFrameworkCore;
using ReadingTracker.Data;
using ReadingTracker.Models;

namespace ReadingTracker.Services;

public class BookService : IBookService
{
    private readonly ApplicationDbContext _context;

    public BookService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Book>> GetBooksAsync() =>
        await _context.Books.OrderBy(b => b.Title).ToListAsync();

    public async Task<List<Book>> SearchBooksAsync(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetBooksAsync();

        var term = searchTerm.Trim();
        return await _context.Books
            .Where(b => EF.Functions.Like(b.Title, $"%{term}%")
                     || EF.Functions.Like(b.Author, $"%{term}%"))
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public async Task<Book?> GetBookAsync(int id) =>
        await _context.Books.FindAsync(id);

    public async Task<Book> CreateBookAsync(Book book)
    {
        book.CreatedAt = DateTime.UtcNow;
        book.UpdatedAt = null;
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<bool> UpdateBookAsync(Book book)
    {
        var existing = await _context.Books.FindAsync(book.Id);
        if (existing is null) return false;

        existing.Title = book.Title;
        existing.Author = book.Author;
        existing.Genre = book.Genre;
        existing.Status = book.Status;
        existing.Rating = book.Rating;
        existing.ReadingNotes = book.ReadingNotes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        var existing = await _context.Books.FindAsync(id);
        if (existing is null) return false;

        _context.Books.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeStatusAsync(int id, ReadingStatus status)
    {
        var existing = await _context.Books.FindAsync(id);
        if (existing is null) return false;

        existing.Status = status;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
