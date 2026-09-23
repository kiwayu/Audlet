using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Library;

public sealed record FileMeta(double DurationSec, string? Title, string? Album, string? Artist, IReadOnlyList<Chapter> Chapters);

public sealed class LibraryScanner
{
    private static readonly System.Text.RegularExpressions.Regex DiscPart = new(@"^(cd|disc|disk|part|vol|volume)\s*[-_.]?\s*\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private readonly Func<string, FileMeta> _readMeta;

    public LibraryScanner(Func<string, FileMeta> readMeta) => _readMeta = readMeta;

    public IReadOnlyList<Book> Scan(IEnumerable<string> roots, IProgress<string>? progress = null, IEnumerable<string>? looseFiles = null)
    {
        var books = new List<Book>();
        foreach (var root in roots.Where(Directory.Exists))
            Walk(root, root, books, progress);
        foreach (var file in (looseFiles ?? []).Where(f => File.Exists(f) && AudioExtensions.IsAudio(f)))
            books.Add(Build(Path.GetDirectoryName(file)!, file, [file], progress));
        return books;
    }

    private void Walk(string root, string folder, List<Book> books, IProgress<string>? progress)
    {
        var files = Directory.EnumerateFiles(folder).Where(AudioExtensions.IsAudio).OrderBy(f => f, NaturalComparer.Instance).ToList();
        var subs = Directory.EnumerateDirectories(folder).OrderBy(d => d, NaturalComparer.Instance).ToList();
        var subsWithAudio = subs.Where(HasAudioBelow).ToList();

        // ponytail: a folder holding only CD1/CD2-style sub-folders is one book; any other layout needs a manual merge later
        var discParts = files.Count == 0 && subsWithAudio.Count > 0 &&
                        subsWithAudio.All(d => DiscPart.IsMatch(Path.GetFileName(d)) && !Directory.EnumerateDirectories(d).Any(HasAudioBelow));
        if ((files.Count > 0 && subsWithAudio.Count == 0) || discParts)
        {
            books.Add(Build(root, folder, AllAudioBelow(folder).ToList(), progress));
            return;
        }
        foreach (var file in files)
            books.Add(Build(root, file, [file], progress));
        foreach (var sub in subsWithAudio)
            Walk(root, sub, books, progress);
    }

    private static bool HasAudioBelow(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Any(AudioExtensions.IsAudio);

    private static IEnumerable<string> AllAudioBelow(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(AudioExtensions.IsAudio)
            .OrderBy(f => f, NaturalComparer.Instance);

    private Book Build(string root, string id, List<string> paths, IProgress<string>? progress)
    {
        progress?.Report(id);
        var isFolder = Directory.Exists(id);
        var files = new List<AudioFile>();
        var chapters = new List<Chapter>();
        FileMeta? first = null;
        for (var i = 0; i < paths.Count; i++)
        {
            var meta = _readMeta(paths[i]);
            first ??= meta;
            var title = string.IsNullOrWhiteSpace(meta.Title) ? Path.GetFileNameWithoutExtension(paths[i]) : meta.Title;
            files.Add(new AudioFile(paths[i], meta.DurationSec, title));
            if (meta.Chapters.Count > 0)
                chapters.AddRange(meta.Chapters.Select(c => c with { FileIndex = i }));
            else
                chapters.Add(new Chapter(title, i, 0, meta.DurationSec));
        }

        var parent = Path.GetDirectoryName(id)!;
        var breadcrumb = Path.GetRelativePath(root, parent);
        var fallbackTitle = Path.GetFileNameWithoutExtension(id);
        return new Book
        {
            Id = id,
            Title = isFolder ? (first?.Album is { Length: > 0 } album ? album : fallbackTitle)
                             : (first?.Title is { Length: > 0 } t ? t : fallbackTitle),
            Author = first?.Artist ?? "",
            Breadcrumb = breadcrumb == "." ? "" : breadcrumb,
            Files = files,
            Chapters = chapters,
            AddedUtc = DateTime.UtcNow,
        };
    }
}
