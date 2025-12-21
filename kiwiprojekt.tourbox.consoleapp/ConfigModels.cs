namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxConfig
    {
        public bool DebugMode { get; set; } = false;
        public Dictionary<string, KeyConfig> Keys { get; set; } = new();
        public Dictionary<string, KeyConfig> Combos { get; set; } = new();
        public Dictionary<string, RotaryConfig> Rotary { get; set; } = new();
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
