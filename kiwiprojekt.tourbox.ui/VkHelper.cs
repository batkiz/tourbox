namespace kiwiprojekt.tourbox.ui;

/// <summary>
/// Helpers for VK_ key name convention used throughout the app.
/// </summary>
public static class VkHelper
{
    /// <summary>Strip "VK_" prefix if present, e.g. "VK_CONTROL" → "CONTROL".</summary>
    public static string StripPrefix(string key) =>
        key.StartsWith("VK_", StringComparison.OrdinalIgnoreCase) ? key[3..] : key;

    /// <summary>Add "VK_" prefix if not present, e.g. "CONTROL" → "VK_CONTROL".</summary>
    public static string EnsurePrefix(string key) =>
        key.StartsWith("VK_", StringComparison.OrdinalIgnoreCase) ? key : $"VK_{key}";
}
