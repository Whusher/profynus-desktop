using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace Profynus.Desktop.Controls;

// A perspective-projected spherical mesh. Geometry responds to real output energy.
public sealed class AudioOrb : Control
{
    public float Energy { get; set; }
    public string Profile { get; set; } = "Relax";
    public Color Accent { get; set; } = Color.Parse("#20D7ED");
    public bool ReducedMotion { get; set; }
    private double phase, envelope;
    public void Advance(bool playing)
    {
        envelope += ((playing ? Energy : 0) - envelope) * .18;
        if (!ReducedMotion && playing) phase += .009;
        InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var radius = Math.Min(Bounds.Width, Bounds.Height) * .32;
        var gain = ReducedMotion ? 0 : Profile switch { "Aggressive" => .6, "EDM" => .4, "Soft" => .12, _ => .22 };
        var glow = new SolidColorBrush(Accent, .035);
        for (var i = 4; i > 0; i--) context.DrawEllipse(glow, null, center, radius * (1 + i * .15), radius * (1 + i * .15));
        context.DrawEllipse(null, new Pen(new SolidColorBrush(Accent, .12), 1), center, radius * 1.4, radius * 1.4);
        const int rows = 24, columns = 48;
        var points = new Point[rows + 1, columns + 1];
        var depths = new double[rows + 1, columns + 1];
        for (var y = 0; y <= rows; y++)
        for (var x = 0; x <= columns; x++)
        {
            var lat = Math.PI * y / rows;
            var lon = 2 * Math.PI * x / columns;
            var r = radius * (1 + envelope * gain * (1 + Math.Sin(lon * 7 + lat * 5 + phase * 5)));
            var px = Math.Sin(lat) * Math.Cos(lon + phase);
            var py = Math.Cos(lat);
            var pz = Math.Sin(lat) * Math.Sin(lon + phase);
            var ry = py * .93 - pz * .36;
            var rz = py * .36 + pz * .93;
            var perspective = 3.5 / (3.5 - rz);
            points[y, x] = new(center.X + px * r * perspective, center.Y + ry * r * perspective);
            depths[y, x] = rz;
        }
        for (var y = 0; y < rows; y++)
        for (var x = 0; x < columns; x++)
        {
            var pen = new Pen(new SolidColorBrush(Accent, .12 + (depths[y, x] + 1) * .29), .8);
            context.DrawLine(pen, points[y, x], points[y, x + 1]);
            context.DrawLine(pen, points[y, x], points[y + 1, x]);
            context.DrawLine(pen, points[y, x], points[y + 1, x + 1]);
        }
    }
}
