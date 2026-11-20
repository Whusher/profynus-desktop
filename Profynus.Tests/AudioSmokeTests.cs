using System.Diagnostics;
using Profynus.Desktop.Services;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)] // SDL_mixer is process-global.
namespace Profynus.Tests;
public sealed class AudioSmokeTests
{
    [Fact, Trait("Category", "NativeAudio")]
    public void BundleContainsRealPlatformAudioDrivers()
    {
        var drivers = SdlAudioPlayer.GetAvailableDrivers();
        if (OperatingSystem.IsLinux())
        {
            Assert.Contains("pulseaudio", drivers);
            Assert.Contains("alsa", drivers);
        }
        else if (OperatingSystem.IsWindows()) Assert.Contains("wasapi", drivers);
        else if (OperatingSystem.IsMacOS()) Assert.Contains("coreaudio", drivers);
    }
    [Fact, Trait("Category", "NativeAudio")]
    public async Task PlaybackSupportsPcmPauseResumeSeekVolumeAndCompletion()
    {
        using var fixture = new AudioFixture();
        using var player = new SdlAudioPlayer();
        player.Play(fixture.Wave); player.SetVolume(60);
        await WaitUntil(() => player.Energy > .01f);
        Assert.True(player.IsPlaying); Assert.InRange(player.Duration, 2.9, 3.1);
        player.TogglePause(); Assert.False(player.IsPlaying);
        player.TogglePause(); Assert.True(player.IsPlaying);
        player.Seek(1); await WaitUntil(() => player.Position >= 1);
        player.SetVolume(0); await WaitUntil(() => player.Energy < .01f);
        await WaitUntil(() => !player.IsPlaying, 5000);
    }
    [Fact, Trait("Category", "NativeAudio")]
    public async Task MisnamedWebmIsConvertedPlayedAndCleanedWithoutChangingOriginal()
    {
        using var fixture = new AudioFixture();
        var source = Path.Combine(fixture.Root, "Day and Night.mp3");
        var info = new ProcessStartInfo(AudioRuntime.FFmpegPath) { UseShellExecute = false, RedirectStandardError = true };
        foreach (var arg in new[] { "-nostdin", "-loglevel", "error", "-i", fixture.Wave, "-ac", "2", "-c:a", "vorbis", "-strict", "experimental", "-f", "webm", source }) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)!;
        var errors = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(); Assert.True(process.ExitCode == 0, errors);
        var original = await File.ReadAllBytesAsync(source);
        string temporary;
        using (var prepared = await new AudioFilePreparation().PrepareAsync(source, CancellationToken.None))
        {
            temporary = prepared.Path;
            Assert.NotEqual(source, temporary);
            using var player = new SdlAudioPlayer(); player.Play(temporary);
            await WaitUntil(() => player.Energy > .01f);
            Assert.True(player.IsPlaying);
        }
        Assert.False(File.Exists(temporary));
        Assert.Equal(original, await File.ReadAllBytesAsync(source));
    }
    private static async Task WaitUntil(Func<bool> condition, int timeout = 3000)
    {
        var clock = Stopwatch.StartNew();
        while (!condition() && clock.ElapsedMilliseconds < timeout) await Task.Delay(25);
        Assert.True(condition(), "Audio condition did not become true before timeout.");
    }
}
