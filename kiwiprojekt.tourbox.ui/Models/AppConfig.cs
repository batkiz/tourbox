using System.IO;
using System.Text.Json;

namespace kiwiprojekt.tourbox.ui.Models;

public class AppConfig
{
    public string PortName { get; set; } = "";
    public bool DebugMode { get; set; }
    public bool RequiresInit { get; set; } = true;
    public Dictionary<string, KeyBinding> Keys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, KeyBinding> Combos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RotaryBinding> Rotary { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
            return new AppConfig();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? new AppConfig();
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }
}

public class KeyBinding
{
    public string Action { get; set; } = "";
    public string Mode { get; set; } = "Tap";
    public string Value { get; set; } = "";
}

public class RotaryBinding
{
    public KeyBinding Clockwise { get; set; } = new();
    public KeyBinding CounterClockwise { get; set; } = new();
}
