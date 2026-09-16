using Microsoft.AspNetCore.Mvc;
using ReadingTracker.Models;
using ReadingTracker.Services;

namespace ReadingTracker.Controllers;

public class BooksController : Controller
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var books = await _bookService.SearchBooksAsync(searchTerm);
        ViewData["SearchTerm"] = searchTerm;
        return View(books);
    }

    public IActionResult Create() => View(new Book());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book book)
    {
        if (!ModelState.IsValid)
            return View(book);

        await _bookService.CreateBookAsync(book);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var book = await _bookService.GetBookAsync(id);
        if (book is null) return NotFound();
        return View(book);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Book book)
    {
        if (id != book.Id) return NotFound();
        if (!ModelState.IsValid)
            return View(book);

        var updated = await _bookService.UpdateBookAsync(book);
        if (!updated) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _bookService.DeleteBookAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartReading(int id)
    {
        await _bookService.ChangeStatusAsync(id, ReadingStatus.CurrentlyReading);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        await _bookService.ChangeStatusAsync(id, ReadingStatus.Completed);
        return RedirectToAction(nameof(Index));
    }
}
