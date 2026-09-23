using Audlet.Models;

namespace Audlet.Library;

public static class LibraryMerger
{
    public static void Merge(List<Book> existing, IReadOnlyList<Book> scanned)
    {
        var byId = existing.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var fresh in scanned)
        {
            seen.Add(fresh.Id);
            if (byId.TryGetValue(fresh.Id, out var old))
            {
                old.Files = fresh.Files;
                old.Chapters = fresh.Chapters;
                old.Breadcrumb = fresh.Breadcrumb;
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
        foreach (var book in existing)
            if (!seen.Contains(book.Id)) book.Missing = true;
    }
}
