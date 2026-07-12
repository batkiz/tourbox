namespace kiwiprojekt.tourbox.ui.Models;

/// <summary>
/// A single row in the mapping list display.
/// </summary>
public class MappingRow
{
    public string ControlName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Mode { get; set; } = "";
    public bool IsRotary { get; set; }
}
