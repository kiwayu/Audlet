using System.IO.Compression;

namespace Audlet.Data;

// Copies of the data files: a zip the user chooses, and a rolling daily copy kept beside the data.
public static class Backup
{
    public static readonly string[] Files = ["library.json", "settings.json", "stats.json"];
    public const int KeepDays = 7;

    public static string DailyFolder(string root) => Path.Combine(root, "backups");

    public static void Create(string root, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var name in Files)
        {
            var path = Path.Combine(root, name);
            if (File.Exists(path)) zip.CreateEntryFromFile(path, name);
        }
        var covers = Path.Combine(root, "covers");
        if (Directory.Exists(covers))
            foreach (var cover in Directory.GetFiles(covers, "*.jpg"))
                zip.CreateEntryFromFile(cover, "covers/" + Path.GetFileName(cover));
    }

    // Writes a backup made by Create over the data folder. Only the known file names are accepted,
    // so an entry cannot name a path outside the folder.
    public static void Restore(string root, string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        if (!zip.Entries.Any(e => e.FullName == "library.json")) throw new InvalidDataException("That file is not an Audlet backup.");
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            var isData = Files.Contains(name);
            var isCover = name.StartsWith("covers/") && name.Count(c => c == '/') == 1 && name.EndsWith(".jpg") && !name.Contains("..");
            if (!isData && !isCover) continue;
            var target = Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    // Keeps one copy of the data files per day and prunes the folder to the last KeepDays days.
    public static void Daily(string root, DateTime today)
    {
        var folder = Path.Combine(DailyFolder(root), today.ToString("yyyy-MM-dd"));
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            foreach (var name in Files)
            {
                var path = Path.Combine(root, name);
                if (File.Exists(path)) File.Copy(path, Path.Combine(folder, name), overwrite: true);
            }
        }
        foreach (var old in Directory.GetDirectories(DailyFolder(root)).OrderByDescending(d => d).Skip(KeepDays))
            Directory.Delete(old, recursive: true);
    }
}
