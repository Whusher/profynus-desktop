namespace Profynus.Desktop.Models;

public sealed record Track(string Path, string Title, string Artist, string Format)
{
    public static Track FromPath(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var parts = name.Split(" - ", 2);
        return new(path, parts.Length == 2 ? parts[1] : name,
            parts.Length == 2 ? parts[0] : "Local collection", System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant());
    }
}
