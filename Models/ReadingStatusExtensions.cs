namespace ReadingTracker.Models;

public static class ReadingStatusExtensions
{
    public static string ToDisplayName(this ReadingStatus status) => status switch
    {
        ReadingStatus.WantToRead => "Want to Read",
        ReadingStatus.CurrentlyReading => "Currently Reading",
        ReadingStatus.Completed => "Completed",
        _ => status.ToString()
    };
}
