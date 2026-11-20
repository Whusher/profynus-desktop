namespace Profynus.Tests;
internal sealed class AudioFixture : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("profynus-tests-").FullName;
    public string Wave { get; }
    public AudioFixture()
    {
    var path = Path.Combine(Root, "Artist - Signal.wav");
    using (var writer = new BinaryWriter(File.Create(path)))
    {
        const int size = 44100 * 2 * 3;
        writer.Write("RIFF"u8); writer.Write(36 + size); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(44100); writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(size);
        for (int i = 0; i < size / 2; i++) writer.Write((short)(12000 * Math.Sin(i * 2 * Math.PI * 220 / 44100)));
    }
        Wave = path;
    }
    public void Dispose() => Directory.Delete(Root, true);
}
