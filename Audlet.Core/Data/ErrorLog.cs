namespace Audlet.Data;

// Unhandled exceptions land in errors.log next to the data files, so a crash can be read back later.
public static class ErrorLog
{
    public const int MaxBytes = 512 * 1024;

    public static string FilePath(string root) => Path.Combine(root, "errors.log");

    public static void Write(string root, string where, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(root);
            var path = FilePath(root);
            // Keep the file bounded: once it grows past the cap, drop the older half.
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
            {
                var text = File.ReadAllText(path);
                File.WriteAllText(path, text[(text.Length / 2)..]);
            }
            var nl = Environment.NewLine;
            File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + where + "] Audlet " + UpdateChecker.Current + nl + ex + nl + nl);
        }
        catch (Exception) { }
    }
}
