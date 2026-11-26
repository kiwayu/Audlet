using System.Text.RegularExpressions;
using Audlet.Models;

namespace Audlet.Library;

// Series name and number from tags or folder names, and the next book in a series.
public static class SeriesInfo
{
    // "Dune 03", "Dune - Book 3", "Dune #3", "Dune, Vol. 3"
    private static readonly Regex Trailing = new(@"^(?<series>.+?)[ ,:-]*(?:book|bk|part|vol[.]?|volume|#)?[ ]*(?<n>[0-9]{1,3})$", RegexOptions.IgnoreCase);
    // "03 - Dune", "3. Dune"
    private static readonly Regex Leading = new(@"^(?<n>[0-9]{1,3})[ ]*[-.:][ ]*(?<title>.+)$");

    public static (string? Series, double? Index) Parse(string bookName, string? parentFolder)
    {
        bookName = bookName.Trim();
        var t = Trailing.Match(bookName);
        if (t.Success && t.Groups["series"].Value.Trim().Length > 1)
            return (t.Groups["series"].Value.Trim().TrimEnd('-', ',', ':').Trim(), double.Parse(t.Groups["n"].Value));
        var l = Leading.Match(bookName);
        if (l.Success && !string.IsNullOrWhiteSpace(parentFolder))
            return (parentFolder.Trim(), double.Parse(l.Groups["n"].Value));
        return (null, null);
    }

    public static IEnumerable<Book> InSeries(IEnumerable<Book> books, string series) =>
        books.Where(b => b.Series is not null && b.Series.Equals(series, StringComparison.OrdinalIgnoreCase));

    public static Book? Next(IEnumerable<Book> books, Book current)
    {
        if (current.Series is null || current.SeriesIndex is null) return null;
        return InSeries(books, current.Series)
            .Where(b => b != current && b.SeriesIndex > current.SeriesIndex && !b.Missing)
            .OrderBy(b => b.SeriesIndex).FirstOrDefault();
    }

    // Fills the "Book 3 of 6" labels across the library.
    public static void Label(IEnumerable<Book> books)
    {
        var list = books.ToList();
        foreach (var group in list.Where(b => b.Series is not null).GroupBy(b => b.Series!, StringComparer.OrdinalIgnoreCase))
        {
            var count = group.Count();
            foreach (var b in group)
                b.SeriesLabel = b.SeriesIndex is { } i ? b.Series + " " + i.ToString("0.#") + " of " + count : b.Series;
        }
        foreach (var b in list.Where(b => b.Series is null)) b.SeriesLabel = null;
    }
}
