namespace ReadingTracker.Models;

// EF Core stores enums as plain integers (WantToRead=0, CurrentlyReading=1, Completed=2).
// Reordering or inserting values here would silently change the meaning of existing rows.
public enum ReadingStatus
{
    WantToRead,
    CurrentlyReading,
    Completed
}