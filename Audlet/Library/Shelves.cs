using Audlet.Models;

namespace Audlet.Library;

// The smart shelves in the sidebar. Each is a rule over the library; nothing is stored.
public static class Shelves
{
    public const int RecentDays = 30;
    public const int PausedDays = 30;
    public const double NearlyDoneFraction = 0.75;

    public static bool RecentlyAdded(Book b, DateTime nowUtc) => b.AddedUtc > nowUtc.AddDays(-RecentDays);

    public static bool NearlyDone(Book b) =>
        b.State == BookState.Started && b.TotalDurationSec > 0 && b.BookPositionSec / b.TotalDurationSec >= NearlyDoneFraction;

    public static bool Paused(Book b, DateTime nowUtc) =>
        b.State == BookState.Started && b.LastPlayedUtc is { } last && last < nowUtc.AddDays(-PausedDays);
}
