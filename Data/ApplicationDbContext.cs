using Microsoft.EntityFrameworkCore;
using ReadingTracker.Models;

namespace ReadingTracker.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // EF Core maps this to a "Books" table automatically, based on the property name.
    public DbSet<Book> Books => Set<Book>();
}