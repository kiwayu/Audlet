using Audlet.Models;

namespace Audlet.Library;

// Moves or renames a book on disk and rewrites its paths so state, bookmarks and history stay attached.
public static class BookMover
{
    public static void Relocate(Book book, string newId)
    {
        if (string.Equals(book.Id, newId, StringComparison.OrdinalIgnoreCase)) return;
        if (Directory.Exists(newId) || File.Exists(newId)) throw new IOException("Something already exists at " + newId);
        Directory.CreateDirectory(Path.GetDirectoryName(newId)!);
        if (Directory.Exists(book.Id)) Directory.Move(book.Id, newId);
        else File.Move(book.Id, newId);
        var oldId = book.Id;
        book.Id = newId;
        book.Files = book.Files.Select(f => f with { Path = Rebase(f.Path, oldId, newId) }).ToList();
        book.CoverPath = null;   // cached under a hash of the id, so it is rebuilt on the next scan
    }

    public static string Rename(Book book, string newName)
    {
        var parent = Path.GetDirectoryName(book.Id)!;
        var target = Directory.Exists(book.Id) ? Path.Combine(parent, newName) : Path.Combine(parent, newName + Path.GetExtension(book.Id));
        Relocate(book, target);
        book.Title = newName;
        return target;
    }

    public static string MoveInto(Book book, string folder)
    {
        var target = Path.Combine(folder, Path.GetFileName(book.Id));
        Relocate(book, target);
        return target;
    }

    private static string Rebase(string path, string oldRoot, string newRoot) =>
        path.StartsWith(oldRoot, StringComparison.OrdinalIgnoreCase) ? newRoot + path[oldRoot.Length..] : path;
}
