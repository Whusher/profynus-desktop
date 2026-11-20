namespace Profynus.Desktop.Services;

/// <summary>Hover reveals controls; three seconds outside hides them. Keyboard focus keeps them usable.</summary>
public sealed class MiniPlayerVisibility
{
    private TimeSpan lastInteraction;
    private bool hovered;
    public void Enter(TimeSpan now) { hovered = true; lastInteraction = now; }
    public void Leave(TimeSpan now) { hovered = false; lastInteraction = now; }
    public void Interact(TimeSpan now) => lastInteraction = now;
    public bool ControlsVisible(TimeSpan now, bool keyboardFocus) => hovered || keyboardFocus || now - lastInteraction < TimeSpan.FromSeconds(3);
}
