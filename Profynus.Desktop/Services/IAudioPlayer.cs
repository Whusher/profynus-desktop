namespace Profynus.Desktop.Services;
public interface IAudioPlayer : IDisposable
{
    bool IsPlaying { get; }
    double Position { get; }
    double Duration { get; }
    float Energy { get; }
    void Play(string path);
    void TogglePause();
    void Seek(double seconds);
    void SetVolume(double volume);
}
