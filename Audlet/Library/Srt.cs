using System.Text.RegularExpressions;

namespace Audlet.Library;

public sealed record Cue(double StartSec, double EndSec, string Text);

// Reads a SubRip file that sits beside a book, so the current line can be shown while it plays.
public static partial class Srt
{
    [GeneratedRegex("([0-9]+):([0-9][0-9]):([0-9][0-9])[,.]([0-9]{1,3}) *--> *([0-9]+):([0-9][0-9]):([0-9][0-9])[,.]([0-9]{1,3})")]
    private static partial Regex Timing();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();

    public static IReadOnlyList<Cue> Parse(string text)
    {
        var cues = new List<Cue>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var m = Timing().Match(lines[i]);
            if (!m.Success) continue;
            var body = new List<string>();
            for (i++; i < lines.Length && lines[i].Trim().Length > 0; i++) body.Add(lines[i].Trim());
            cues.Add(new Cue(Seconds(m, 1), Seconds(m, 5), Tags().Replace(string.Join(" ", body), "")));
        }
        return cues;
    }

    private static double Seconds(Match m, int group) =>
        int.Parse(m.Groups[group].Value) * 3600 + int.Parse(m.Groups[group + 1].Value) * 60 + int.Parse(m.Groups[group + 2].Value)
        + int.Parse(m.Groups[group + 3].Value.PadRight(3, '0')) / 1000.0;

    // ponytail: a linear scan is fine for a few thousand cues; switch to a binary search if files get huge.
    public static string? At(IReadOnlyList<Cue> cues, double sec)
    {
        foreach (var c in cues) if (sec >= c.StartSec && sec <= c.EndSec) return c.Text;
        return null;
    }
}
