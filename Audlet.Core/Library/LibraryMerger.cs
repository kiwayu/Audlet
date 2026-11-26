using Audlet.Models;

namespace Audlet.Library;

public static class LibraryMerger
{
    // Books that are no longer found are kept as "missing" while their root is still configured (an unplugged drive),
    // and dropped once the root itself has been removed from Settings.
    public static void Merge(List<Book> existing, IReadOnlyList<Book> scanned, IEnumerable<string>? roots = null, IEnumerable<string>? looseFiles = null)
    {
        var rootList = roots?.Select(r => r.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).ToList();
        var looseSet = new HashSet<string>(looseFiles ?? [], StringComparer.OrdinalIgnoreCase);
        var byId = existing.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fresh in scanned)
        {
            seen.Add(fresh.Id);
            if (byId.TryGetValue(fresh.Id, out var old))
            {
                old.Files = fresh.Files;
                old.Chapters = fresh.Chapters;
                old.Companions = fresh.Companions;
                old.Breadcrumb = fresh.Breadcrumb;
                old.Series = fresh.Series;
                old.SeriesIndex = fresh.SeriesIndex;
                if (string.IsNullOrWhiteSpace(old.Title)) old.Title = fresh.Title;
                if (string.IsNullOrWhiteSpace(old.Author)) old.Author = fresh.Author;
                old.Missing = false;
                if (old.FileIndex >= old.Files.Count) { old.FileIndex = 0; old.PositionSec = 0; }
            }
            else
            {
                existing.Add(fresh);
                byId[fresh.Id] = fresh;
            }
        }
        foreach (var book in existing.ToList())
        {
            if (seen.Contains(book.Id)) continue;
            var stillConfigured = rootList is null || looseSet.Contains(book.Id) || rootList.Any(r => book.Id.StartsWith(r, StringComparison.OrdinalIgnoreCase));
            if (stillConfigured) book.Missing = true;
            else existing.Remove(book);
        }
    }
}
