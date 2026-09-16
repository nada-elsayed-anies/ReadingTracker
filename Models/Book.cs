using System.ComponentModel.DataAnnotations;

namespace ReadingTracker.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Author { get; set; } = string.Empty;

    [Required]
    public string Genre { get; set; } = string.Empty;

    public ReadingStatus Status { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? ReadingNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}