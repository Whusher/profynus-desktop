using Profynus.Desktop.Services;
using Xunit;
namespace Profynus.Tests;
public sealed class AudioRuntimeTests
{
    [Fact]
    public void MissingBundleFailsInsteadOfUsingSystemInstallation()
    {
        using var fixture = new AudioFixture();
        var error = Assert.Throws<FileNotFoundException>(() => AudioRuntime.RequireFile(fixture.Root, "ffmpeg"));
        Assert.Contains("bundle is incomplete", error.Message);
    }
    [Fact]
    public void ResolverUsesExactApplicationOwnedFile()
    {
        using var fixture = new AudioFixture();
        var path = Path.Combine(fixture.Root, "ffmpeg");
        File.WriteAllText(path, "test fixture");
        Assert.Equal(path, AudioRuntime.RequireFile(fixture.Root, "ffmpeg"));
    }
}
