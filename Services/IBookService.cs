using ReadingTracker.Models;

namespace ReadingTracker.Services;

public interface IBookService
{
    Task<List<Book>> GetBooksAsync();
    Task<List<Book>> SearchBooksAsync(string? searchTerm);
    Task<Book?> GetBookAsync(int id);
    Task<Book> CreateBookAsync(Book book);
    Task<bool> UpdateBookAsync(Book book);
    Task<bool> DeleteBookAsync(int id);
    Task<bool> ChangeStatusAsync(int id, ReadingStatus status);
}
