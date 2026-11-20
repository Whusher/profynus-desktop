using System.Runtime.InteropServices;

namespace Profynus.Desktop.Services;

/// <summary>Only application-owned audio binaries are loaded. Never falls back to PATH/system codecs.</summary>
public static class AudioRuntime
{
    public static string RuntimeId => RuntimeInformation.RuntimeIdentifier;
    public static string DirectoryPath => Path.Combine(AppContext.BaseDirectory, "audio");
    public static string FFmpegPath => RequireFile(DirectoryPath, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    public static string LibraryPath(string library) => RequireFile(DirectoryPath,
        OperatingSystem.IsWindows() ? library + ".dll" : OperatingSystem.IsMacOS()
            ? "lib" + library + ".dylib" : "lib" + library + "-2.0.so.0");

    public static string RequireFile(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
            throw new FileNotFoundException("The Profynus audio bundle is incomplete. Reinstall the complete application package. Developers: build the audio runtime before launching.", path);
        return path;
    }
}
