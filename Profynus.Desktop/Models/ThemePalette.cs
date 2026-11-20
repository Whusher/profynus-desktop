using Avalonia.Media;
namespace Profynus.Desktop.Models;
public sealed record ThemePalette(string Name, string Hex, string Surface)
{
    public IBrush Swatch => Brush.Parse(Hex);
    public override string ToString() => Name;
    public static ThemePalette[] All { get; } =
    [
        new("Profynus Cyan", "#20D7ED", "#080F18"),
        new("Aurora", "#7FFFD4", "#071411"),
        new("Ultraviolet", "#B49AFF", "#100D1C"),
        new("Rose Quartz", "#FFA3C7", "#190D15"),
        new("Solar Flare", "#FFB45B", "#181109"),
        new("Ocean", "#75B9FF", "#09121E"),
        new("Acid", "#D0F574", "#11150A"),
        new("Cherry", "#FF7185", "#1A0C10"),
        new("Glacier", "#B1EFFF", "#0B161C"),
        new("Orchid", "#E99BFF", "#190E1D"),
        new("Copper", "#E9AA80", "#1B120E"),
        new("Mint", "#64E8AC", "#081710"),
        new("Lavender", "#C8BEFF", "#12101C"),
        new("Coral", "#FF9C8F", "#1C100E"),
        new("Gold", "#F4D47A", "#191609"),
        new("Monochrome", "#E4E8ED", "#111317"),
        new("Electric Blue", "#7B94FF", "#0D1020"),
        new("Peach", "#FFCCA9", "#1B130F"),
        new("Sakura", "#F5B6D8", "#1B1018"),
        new("Lagoon", "#55DFD1", "#081815"),
        new("Amethyst", "#CE91EF", "#170D1C"),
        new("Desert", "#D9C39D", "#18150F"),
        new("Arctic", "#92D1E8", "#0A151B"),
        new("Neon Lime", "#A9ED55", "#101708")
    ];
}
