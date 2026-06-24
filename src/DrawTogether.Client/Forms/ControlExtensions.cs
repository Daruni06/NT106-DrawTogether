using System.Windows.Forms;

internal static class ControlExtensions
{
    public static void DoubleBuffered(this Control control, bool enabled)
    {
        var property = typeof(Control).GetProperty(
            "DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        property?.SetValue(control, enabled, null);
    }
}
