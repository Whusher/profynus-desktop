using System.ComponentModel;
using System.Diagnostics;

namespace Profynus.Desktop.Services;

/// <summary>Creates session-owned PCM only for containers SDL_mixer cannot read.</summary>
public sealed class PreparedAudio(string path, string? temporaryDirectory = null) : IDisposable
{
    public string Path { get; } = path;
    public void Dispose()
    {
        if (temporaryDirectory is null) return;
        try { Directory.Delete(temporaryDirectory, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

public sealed class AudioFilePreparation
{
    public static bool NeedsConversion(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> header = stackalloc byte[12];
        var count = file.Read(header);
        // Only PCM WAV is sent directly to our deliberately WAV-only mixer.
        // Every other format is probed by FFmpeg using content, not its extension.
        return !(count >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WAVE"u8));
    }

    public async Task<PreparedAudio> PrepareAsync(string source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        source = Path.GetFullPath(source);
        if (!NeedsConversion(source)) return new(source);
        var directory = Directory.CreateTempSubdirectory("profynus-audio-").FullName;
        var result = new PreparedAudio(Path.Combine(directory, "audio.wav"), directory);
        try
        {
            var info = new ProcessStartInfo(AudioRuntime.FFmpegPath)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            // ArgumentList preserves spaces and treats filenames as data. Only local files are allowed.
            foreach (var argument in new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-y",
                "-protocol_whitelist", "file,pipe", "-i", source, "-map", "0:a:0", "-vn", "-ac", "2",
                "-ar", "44100", "-c:a", "pcm_s16le", result.Path }) info.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = info };
            try { process.Start(); }
            catch (Win32Exception e) { throw new InvalidOperationException("The bundled audio decoder could not start. Reinstall the complete Profynus package. Your original file is unchanged.", e); }
            var errors = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(3));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
                if (!cancellationToken.IsCancellationRequested) throw new InvalidOperationException("Audio preparation timed out. Try a shorter file.");
                throw;
            }
            var error = await errors;
            if (process.ExitCode != 0) throw new InvalidOperationException("Unable to decode this audio file. " + error[..Math.Min(error.Length, 600)]);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch { result.Dispose(); throw; }
    }
}
