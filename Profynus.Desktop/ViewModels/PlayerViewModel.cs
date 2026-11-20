using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Profynus.Desktop.Models;
using Profynus.Desktop.Services;
namespace Profynus.Desktop.ViewModels;

public sealed class PlayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly LibraryService library = new();
    private readonly SettingsStore store = new();
    private readonly CancellationTokenSource lifetime = new();
    public IAudioPlayer Audio { get; } = new SdlAudioPlayer();
    public UserSettings Settings { get; }
    public ObservableCollection<Track> Tracks { get; } = [];
    private List<Track> all = [];
    private List<Track> queue = [];
    public ThemePalette[] Themes => ThemePalette.All;
    public string[] Profiles => ["Relax", "Soft", "EDM", "Aggressive"];
    private string search = "";
    public string Search { get => search; set { search = value; Filter(); Changed(); } }
    private string status = "Your music. Your atmosphere.";
    public string Status { get => status; private set { status = value; Changed(); } }
    private Track? current;
    public Track? Current { get => current; private set { current = value; Changed(); Changed(nameof(Title)); Changed(nameof(Artist)); } }
    public string Title => Current?.Title ?? "Find your frequency.";
    public string Artist => Current?.Artist ?? "Bring your local collection to life";
    public string LibraryCount => $"{all.Count} tracks · on your device";
    public bool IsEmpty => Tracks.Count == 0;
    public bool IsPlaying => Audio.IsPlaying;
    public string PlayLabel => IsPlaying ? "Ⅱ" : "▶";
    public double Duration => Math.Max(1, Audio.Duration);
    public double Position => Audio.Position;
    public string Time => Format(Position) + "  /  " + Format(Audio.Duration);
    private static string Format(double seconds) => TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
    public double Volume { get => Settings.Volume; set { Settings.Volume = value; Audio.SetVolume(value); Changed(); } }
    public ThemePalette Theme
    {
        get => Themes.FirstOrDefault(t => t.Name == Settings.Theme) ?? Themes[0];
        set
        {
            if (value is null) return;
            Settings.Theme = value.Name;
            Application.Current!.Resources["Accent"] = value.Swatch;
            Application.Current.Resources["Canvas"] = Brush.Parse(value.Surface);
            Changed(); Save();
        }
    }
    public string Profile { get => Settings.Profile; set { Settings.Profile = value; Changed(); Save(); } }
    public bool ReducedMotion { get => Settings.ReducedMotion; set { Settings.ReducedMotion = value; Changed(); Save(); } }
    private bool scanning;
    private bool ended;
    public PlayerViewModel() { Settings = store.Load(); Theme = Theme; }
    public async Task ScanAsync(string? folder = null)
    {
        if (scanning) return;
        scanning = true;
        if (folder is not null && !Settings.Folders.Contains(folder)) Settings.Folders.Add(folder);
        Status = "Discovering your music…";
        try
        {
            all = await library.ScanAsync(LibraryService.DefaultFolders().Concat(Settings.Folders), lifetime.Token);
            Filter(); Changed(nameof(LibraryCount));
            Status = all.Count == 0 ? "Add a music folder to begin. Your files stay on your device." : $"Found {all.Count} tracks. Select a song to listen.";
            Save();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Status = e.Message; }
        finally { scanning = false; }
    }
    private void Filter()
    {
        Tracks.Clear();
        foreach (var track in all.Where(t => (t.Title + " " + t.Artist + " " + t.Format).Contains(search, StringComparison.OrdinalIgnoreCase))) Tracks.Add(track);
        Changed(nameof(IsEmpty));
    }
    private readonly AudioFilePreparation preparation = new();
    private CancellationTokenSource? pendingPlayback;
    private PreparedAudio? preparedAudio;
    private bool preparing;
    public async void Play(Track track, bool newQueue = true) => await PlayAsync(track, newQueue);
    public async Task PlayAsync(Track track, bool newQueue = true)
    {
        pendingPlayback?.Cancel();
        using var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        pendingPlayback = request;
        preparing = true;
        Status = "Preparing audio…";
        PreparedAudio? next = null;
        var nextQueue = newQueue ? Tracks.ToList() : queue;
        try
        {
            next = await preparation.PrepareAsync(track.Path, request.Token);
            request.Token.ThrowIfCancellationRequested();
            Audio.Play(next.Path);
            preparedAudio?.Dispose(); preparedAudio = next; next = null;
            Audio.SetVolume(Volume); Current = track; queue = nextQueue;
            wasPlaying = true; ended = false;
            Status = $"Playing locally · {track.Format}"; Refresh();
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (!request.IsCancellationRequested) Status = e.Message; }
        finally
        {
            next?.Dispose();
            if (ReferenceEquals(pendingPlayback, request)) { pendingPlayback = null; preparing = false; }
        }
    }
    public void Toggle()
    {
        if (Current is null) { if (Tracks.Count > 0) Play(Tracks[0]); return; }
        if (ended) { Play(Current, false); return; }
        Audio.TogglePause(); wasPlaying = Audio.IsPlaying; Refresh();
    }
    public void Next(int direction)
    {
        if (queue.Count == 0) return;
        var index = queue.IndexOf(Current!);
        Play(queue[(index + direction + queue.Count) % queue.Count], false);
    }
    public void Seek(double seconds) { try { Audio.Seek(seconds); } catch (Exception e) { Status = e.Message; } }
    private bool wasPlaying;
    public void Tick()
    {
        if (!preparing && wasPlaying && !IsPlaying)
        {
            wasPlaying = false; ended = true;
            var index = queue.IndexOf(Current!);
            if (index >= 0 && index + 1 < queue.Count) Play(queue[index + 1], false);
        }
        Refresh();
    }
    private void Refresh() { Changed(nameof(IsPlaying)); Changed(nameof(PlayLabel)); Changed(nameof(Position)); Changed(nameof(Duration)); Changed(nameof(Time)); }
    private void Save() { try { store.Save(Settings); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Status = "Settings could not be saved: " + e.Message; } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
    public void Dispose() { lifetime.Cancel(); Save(); Audio.Dispose(); preparedAudio?.Dispose(); lifetime.Dispose(); }
}
