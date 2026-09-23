using Audlet.Models;

namespace Audlet.Player;

public static class History
{
    public static void Record(Book book, HistoryKind kind, int fileIndex, double posSec, DateTime nowUtc)
    {
        book.History.Insert(0, new HistoryEntry(kind, fileIndex, posSec, nowUtc));
        if (book.History.Count > Book.HistoryCap)
            book.History.RemoveRange(Book.HistoryCap, book.History.Count - Book.HistoryCap);
    }
}
