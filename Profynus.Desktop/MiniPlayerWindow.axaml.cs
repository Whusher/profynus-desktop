using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Profynus.Desktop.Services;
using Profynus.Desktop.ViewModels;
namespace Profynus.Desktop;

public partial class MiniPlayerWindow : Window
{
    private readonly PlayerViewModel model = null!;
    private readonly Action restore = () => { };
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly MiniPlayerVisibility visibility = new();
    private bool keyboardInteraction;
    public MiniPlayerWindow() => InitializeComponent();
    public MiniPlayerWindow(PlayerViewModel model, Action restore) : this()
    {
        this.model = model; this.restore = restore; DataContext = model;
        PointerEntered += (_, _) => visibility.Enter(clock.Elapsed);
        PointerExited += (_, _) => { visibility.Leave(clock.Elapsed); keyboardInteraction = false; };
        KeyDown += (_, e) =>
        {
            keyboardInteraction = true; visibility.Interact(clock.Elapsed);
            if (e.Key == Key.Escape) Close();
        };
        Deactivated += (_, _) => keyboardInteraction = false;
        timer.Tick += (_, _) =>
        {
            var visible = visibility.ControlsVisible(clock.Elapsed, keyboardInteraction && IsActive);
            // Opacity retains the layout so the globe/title never jump when controls disappear.
            WindowActions.Opacity = PlaybackControls.Opacity = visible ? 1 : 0;
            WindowActions.IsHitTestVisible = PlaybackControls.IsHitTestVisible = visible;
            Orb.Energy = model.Audio.Energy; Orb.Profile = model.Profile;
            Orb.Accent = Color.Parse(model.Theme.Hex); Orb.ReducedMotion = model.ReducedMotion;
            Orb.Advance(model.IsPlaying);
        };
        Opened += (_, _) => timer.Start();
        Closed += (_, _) => timer.Stop();
    }
    private void DragWindow(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }
    private void Restore(object? sender, RoutedEventArgs e) => restore();
    private void Dismiss(object? sender, RoutedEventArgs e) => Close();
    private void TogglePlay(object? sender, RoutedEventArgs e) => model.Toggle();
    private void Previous(object? sender, RoutedEventArgs e) => model.Next(-1);
    private void Next(object? sender, RoutedEventArgs e) => model.Next(1);
}
