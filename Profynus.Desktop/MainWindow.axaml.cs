using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Profynus.Desktop.Models;
using Profynus.Desktop.ViewModels;
namespace Profynus.Desktop;

public partial class MainWindow : Window
{
    private readonly PlayerViewModel model;
    private readonly DispatcherTimer timer;
    private int ticks;
    private bool seeking;
    private MiniPlayerWindow? miniPlayer;
    private bool shuttingDown;
    public MainWindow()
    {
        InitializeComponent();
        model = new PlayerViewModel(); DataContext = model;
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        timer.Tick += (_, _) =>
        {
            Orb.Energy = model.Audio.Energy; Orb.Profile = model.Profile;
            Orb.Accent = Color.Parse(model.Theme.Hex); Orb.ReducedMotion = model.ReducedMotion;
            Orb.Advance(model.IsPlaying);
            if (++ticks % 15 == 0 && !seeking) model.Tick();
        };
        SeekBar.AddHandler(PointerPressedEvent, (_, _) => seeking = true, RoutingStrategies.Tunnel);
        SeekBar.AddHandler(PointerReleasedEvent, (_, _) => { model.Seek(SeekBar.Value); seeking = false; }, RoutingStrategies.Tunnel);
        SeekBar.PointerCaptureLost += (_, _) => seeking = false;
        SeekBar.AddHandler(KeyUpEvent, (_, _) => model.Seek(SeekBar.Value), RoutingStrategies.Bubble);
        Opened += async (_, _) => { timer.Start(); await model.ScanAsync(); };
        Closed += (_, _) => { shuttingDown = true; miniPlayer?.Close(); timer.Stop(); model.Dispose(); };
    }
    private void RestoreMainPlayer()
    {
        WindowState = WindowState.Normal;
        Show(); Activate();
    }
    private void OpenMiniPlayer(object? sender, RoutedEventArgs e)
    {
        if (miniPlayer is null)
        {
            miniPlayer = new MiniPlayerWindow(model, RestoreMainPlayer);
            miniPlayer.Closed += (_, _) => { miniPlayer = null; if (!shuttingDown) RestoreMainPlayer(); };
            miniPlayer.Show(); // Unowned so minimizing the library does not minimize the overlay.
        }
        else miniPlayer.Activate();
        WindowState = WindowState.Minimized;
    }
    private async void AddFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose a music folder", AllowMultiple = false });
        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path) await model.ScanAsync(path);
    }
    private async void Rescan(object? sender, RoutedEventArgs e) => await model.ScanAsync();
    private void TogglePlay(object? sender, RoutedEventArgs e) => model.Toggle();
    private void Next(object? sender, RoutedEventArgs e) => model.Next(1);
    private void Previous(object? sender, RoutedEventArgs e) => model.Next(-1);
    private void PlaySelected(object? sender, TappedEventArgs e) { if (Library.SelectedItem is Track track) model.Play(track); }
    private void LibraryKeyDown(object? sender, KeyEventArgs e) { if (e.Key == Key.Enter && Library.SelectedItem is Track track) { model.Play(track); e.Handled = true; } }
}
