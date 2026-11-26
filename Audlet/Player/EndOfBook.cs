using Audlet.Library;
using Audlet.Models;

namespace Audlet.Player;

// What follows the end of a book: it is marked finished and leaves the queue, and the next book is the head of the
// queue, or else the next one in its series. Next is null when nothing should load.
public static class EndOfBook
{
    public static (Book? Next, bool Play, string Toast) Finish(Book finished, IReadOnlyList<Book> books, Settings settings)
    {
        finished.State = BookState.Finished;
        finished.FileIndex = 0; finished.PositionSec = 0;
        settings.Queue.RemoveAll(id => id.Equals(finished.Id, StringComparison.OrdinalIgnoreCase));
        var action = settings.EndOfBookAction;
        var nextId = settings.Queue.FirstOrDefault();
        var next = nextId is null ? null : books.FirstOrDefault(b => b.Id.Equals(nextId, StringComparison.OrdinalIgnoreCase));
        var fromQueue = next is not null;
        next ??= SeriesInfo.Next(books, finished);   // nothing queued: fall through to the next book in the series
        if (action != EndOfBookAction.Stop && next is not null)
        {
            if (fromQueue) settings.Queue.RemoveAt(0);
            return (next, action == EndOfBookAction.NextBookPlay, "Finished. Next: " + next.Title);
        }
        return (null, false, next is null ? "The book is finished." : "Finished. Next in the series: " + next.Title);
    }
}
