using Profynus.Desktop.Models;
namespace Profynus.Desktop.Services;

public sealed class LibraryService
{
    public static readonly string[] Extensions = [".mp3", ".flac", ".wav", ".ogg", ".opus", ".aiff", ".aif", ".webm", ".m4a", ".aac"];
    public static IEnumerable<string> DefaultFolders()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var folders = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), System.IO.Path.Combine(home, "Music") };
        var config = System.IO.Path.Combine(home, ".config", "user-dirs.dirs");
        if (File.Exists(config))
        {
            var line = File.ReadLines(config).FirstOrDefault(l => l.StartsWith("XDG_MUSIC_DIR="));
            if (line is not null) folders.Add(line.Split('=', 2)[1].Trim('"').Replace("$HOME", home));
        }
        return folders.Where(Directory.Exists).Distinct();
    }
    public Task<List<Track>> ScanAsync(IEnumerable<string> folders, CancellationToken cancellationToken) => Task.Run(() =>
    {
        var paths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (var folder in folders.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(folder)) continue;
            try
            {
                foreach (var path in Directory.EnumerateFiles(folder, "*", new EnumerationOptions
                { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (Extensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) paths.Add(path);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return paths.Select(Track.FromPath).OrderBy(t => t.Artist).ThenBy(t => t.Title).ToList();
    }, cancellationToken);
}
