namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxConfig
    {
        public string PortName { get; set; } = "";
        public bool DebugMode { get; set; } = false;
        public bool RequiresInit { get; set; } = true;
        public Dictionary<string, KeyConfig> Keys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, KeyConfig> Combos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, RotaryConfig> Rotary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class KeyConfig
    {
        public string Action { get; set; } = "";
        public string Mode { get; set; } = "Tap"; // Tap, Hold
        public string Value { get; set; } = ""; // context dependent param
    }

    public class RotaryConfig
    {
        public KeyConfig Clockwise { get; set; } = new();
        public KeyConfig CounterClockwise { get; set; } = new();
    }
}
