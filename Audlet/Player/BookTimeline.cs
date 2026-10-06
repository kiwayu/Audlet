using Audlet.Models;

namespace Audlet.Player;

public sealed class BookTimeline
{
    private readonly Book _book;
    private readonly double[] _offsets;   // start of each file in book seconds

    public BookTimeline(Book book)
    {
        _book = book;
        _offsets = new double[book.Files.Count];
        double acc = 0;
        for (var i = 0; i < book.Files.Count; i++) { _offsets[i] = acc; acc += book.Files[i].DurationSec; }
        Total = acc;
    }

    public double Total { get; }

    public double ToBookSec(int fileIndex, double posSec) =>
        _offsets.Length == 0 ? 0 : _offsets[Math.Clamp(fileIndex, 0, _offsets.Length - 1)] + posSec;

    public (int FileIndex, double PosSec) FromBookSec(double bookSec)
    {
        if (_offsets.Length == 0) return (0, 0);
        bookSec = Math.Clamp(bookSec, 0, Total);
        for (var i = _offsets.Length - 1; i >= 0; i--)
            if (bookSec >= _offsets[i])
                return (i, Math.Min(bookSec - _offsets[i], _book.Files[i].DurationSec));
        return (0, 0);
    }

    public int ChapterIndexAt(int fileIndex, double posSec)
    {
        var best = -1;
        for (var i = 0; i < _book.Chapters.Count; i++)
        {
            var c = _book.Chapters[i];
            if (c.FileIndex < fileIndex || (c.FileIndex == fileIndex && c.StartSec <= posSec)) best = i;
            else break;
        }
        return best;
    }

    public Chapter? ChapterAt(int fileIndex, double posSec)
    {
        var i = ChapterIndexAt(fileIndex, posSec);
        return i < 0 ? null : _book.Chapters[i];
    }

    // Start of the chapter before the current one, or of the current one when more than graceSec into it. Null with no chapters.
    public (int FileIndex, double PosSec)? PrevChapterStart(int fileIndex, double posSec, double graceSec = 3)
    {
        var i = ChapterIndexAt(fileIndex, posSec);
        if (i < 0) return null;
        var c = _book.Chapters[i];
        if (i > 0 && ToBookSec(fileIndex, posSec) - ToBookSec(c.FileIndex, c.StartSec) <= graceSec) c = _book.Chapters[i - 1];
        return (c.FileIndex, c.StartSec);
    }

    // Start of the next chapter, or null when the current one is the last.
    public (int FileIndex, double PosSec)? NextChapterStart(int fileIndex, double posSec)
    {
        var i = ChapterIndexAt(fileIndex, posSec) + 1;
        if (i >= _book.Chapters.Count) return null;
        return (_book.Chapters[i].FileIndex, _book.Chapters[i].StartSec);
    }

    public (int FileIndex, double PosSec) Offset(int fileIndex, double posSec, double deltaSec) =>
        FromBookSec(ToBookSec(fileIndex, posSec) + deltaSec);
}
