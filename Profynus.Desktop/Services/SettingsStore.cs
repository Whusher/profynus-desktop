using System.Text.Json;
namespace Profynus.Desktop.Services;
public sealed class UserSettings
{
    public string Theme { get; set; } = "Profynus Cyan";
    public string Profile { get; set; } = "Relax";
    public double Volume { get; set; } = 70;
    public List<string> Folders { get; set; } = [];
    public bool ReducedMotion { get; set; }
}
public sealed class SettingsStore
{
    private readonly string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), "Profynus", "settings.json");
    public UserSettings Load()
    {
        try { return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path)) ?? new(); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(path + ".tmp", path, true);
    }
}
