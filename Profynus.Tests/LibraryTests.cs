using Profynus.Desktop.Services;
using Xunit;
namespace Profynus.Tests;
public sealed class LibraryTests
{
    [Fact]
    public async Task DiscoveryDeduplicatesRootsAndIgnoresNonAudio()
    {
        using var fixture = new AudioFixture();
        File.WriteAllText(Path.Combine(fixture.Root, "ignore.txt"), "ignore");
        var tracks = await new LibraryService().ScanAsync([fixture.Root, fixture.Root, Path.Combine(fixture.Root, "missing")], CancellationToken.None);
        var track = Assert.Single(tracks);
        Assert.Equal("Signal", track.Title); Assert.Equal("Artist", track.Artist);
    }
    [Fact]
    public async Task DiscoveryHonorsCancellation()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new LibraryService().ScanAsync([], cts.Token));
    }
    [Theory]
    [InlineData("webm")]
    [InlineData("mp4")]
    public void ContainerDetectionDoesNotTrustMp3Extension(string kind)
    {
        using var fixture = new AudioFixture();
        var path = Path.Combine(fixture.Root, "misnamed.mp3");
        File.WriteAllBytes(path, kind == "webm" ? [0x1a, 0x45, 0xdf, 0xa3] : [0, 0, 0, 24, 102, 116, 121, 112]);
        Assert.True(AudioFilePreparation.NeedsConversion(path));
        Assert.False(AudioFilePreparation.NeedsConversion(fixture.Wave));
    }
    [Fact]
    public async Task NativeFilesAreNotCopiedOrDeleted()
    {
        using var fixture = new AudioFixture();
        using (var prepared = await new AudioFilePreparation().PrepareAsync(fixture.Wave, CancellationToken.None))
            Assert.Equal(fixture.Wave, prepared.Path);
        Assert.True(File.Exists(fixture.Wave));
    }
}
