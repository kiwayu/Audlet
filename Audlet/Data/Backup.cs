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
        // Keep what is about to be replaced, so a wrong zip can be undone from the backups folder.
        var keep = Path.Combine(DailyFolder(root), DateTime.Now.ToString("yyyy-MM-dd") + "-before-restore-" + DateTime.Now.ToString("HHmmss"));
        Directory.CreateDirectory(keep);
        foreach (var name in Files)
            if (File.Exists(Path.Combine(root, name))) File.Copy(Path.Combine(root, name), Path.Combine(keep, name), overwrite: true);
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
    // With a second folder set, a zip of the whole data set is kept there as well, pruned the same way.
    public static void Daily(string root, DateTime today, string? secondFolder = null)
    {
        if (secondFolder is not null && Directory.Exists(secondFolder))
        {
            var zip = Path.Combine(secondFolder, "Audlet-backup-" + today.ToString("yyyy-MM-dd") + ".zip");
            if (!File.Exists(zip)) Create(root, zip);
            foreach (var old in Directory.GetFiles(secondFolder, "Audlet-backup-*.zip").OrderByDescending(f => f).Skip(KeepDays)) File.Delete(old);
        }
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
