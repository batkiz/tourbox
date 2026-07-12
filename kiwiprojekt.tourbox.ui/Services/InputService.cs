using WindowsInput;
using kiwiprojekt.tourbox.ui.Models;

namespace kiwiprojekt.tourbox.ui.Services;

/// <summary>
/// Executes mapped actions when TourBox events fire.
/// Handles Tap/Hold modes, key combos, mouse actions, and text entry.
/// </summary>
public class InputService : IDisposable
{
    private readonly TourBoxService _tourBoxService;
    private readonly IInputSimulator _input = new InputSimulator();
    private readonly HashSet<TourBoxKey> _pressedKeys = new();
    private readonly HashSet<TourBoxKey> _consumedKeys = new();
    private readonly object _lock = new();
    private AppConfig _config = new();

    public InputService(TourBoxService tourBoxService)
    {
        _tourBoxService = tourBoxService;
        _tourBoxService.ButtonEvent += OnTourBoxEvent;
    }

    /// <summary>
    /// Update the active config. Called when mappings are saved.
    /// </summary>
    public void UpdateConfig(AppConfig config)
    {
        _config = config;
    }

    private void OnTourBoxEvent(TourBoxEvent e)
    {
        if (_config.DebugMode)
        {
            System.Diagnostics.Debug.WriteLine($"[InputService] {e}");
        }

        switch (e.Action)
        {
            case ActionType.Click:
                HandleButton(e, isDown: true);
                break;
            case ActionType.ClickReleased:
                HandleButton(e, isDown: false);
                break;
            case ActionType.Increased:
                HandleRotary(e, clockwise: true);
                break;
            case ActionType.Decreased:
                HandleRotary(e, clockwise: false);
                break;
        }
    }

    private void HandleButton(TourBoxEvent e, bool isDown)
    {
        if (e.Keys.Length == 0) return;
        var key = e.Keys[0];

        lock (_lock)
        {
            if (isDown)
            {
                _pressedKeys.Add(key);

                // 1. Check combos: any combo whose all keys are currently pressed
                foreach (var (comboStr, comboBinding) in _config.Combos)
                {
                    var comboKeys = ParseComboKeys(comboStr);
                    if (comboKeys.Count == 0) continue;

                    // Only fire if this key is part of the combo AND all combo keys are pressed
                    if (comboKeys.Contains(key) && comboKeys.All(k => _pressedKeys.Contains(k)))
                    {
                        ExecuteAction(comboBinding, isDown: true);
                        _consumedKeys.UnionWith(comboKeys);
                        return;
                    }
                }

                // 2. Single key action
                if (_consumedKeys.Contains(key)) return;

                if (_config.Keys.TryGetValue(key.ToString(), out var keyBinding))
                {
                    if (IsHoldMode(keyBinding))
                        ExecuteAction(keyBinding, isDown: true);
                }
            }
            else // Key released
            {
                _pressedKeys.Remove(key);

                // Handle consumed keys (from combos)
                if (_consumedKeys.Contains(key))
                {
                    _consumedKeys.Remove(key);

                    // Release Hold-mode keys that were part of a combo
                    if (_config.Keys.TryGetValue(key.ToString(), out var consumedBinding))
                    {
                        if (IsHoldMode(consumedBinding))
                            ExecuteAction(consumedBinding, isDown: false);
                    }
                    return;
                }

                // Single key action on release
                if (_config.Keys.TryGetValue(key.ToString(), out var releaseBinding))
                {
                    if (IsTapMode(releaseBinding))
                        ExecuteAction(releaseBinding, isDown: null);
                    else if (IsHoldMode(releaseBinding))
                        ExecuteAction(releaseBinding, isDown: false);
                }
            }
        }
    }

    private static List<TourBoxKey> ParseComboKeys(string comboStr)
    {
        var keys = new List<TourBoxKey>();
        foreach (var part in comboStr.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<TourBoxKey>(part, true, out var key))
                keys.Add(key);
        }
        return keys;
    }

    private static bool IsHoldMode(KeyBinding cfg)
        => string.Equals(cfg.Mode, "Hold", StringComparison.OrdinalIgnoreCase);

    private static bool IsTapMode(KeyBinding cfg)
        => string.IsNullOrWhiteSpace(cfg.Mode) || string.Equals(cfg.Mode, "Tap", StringComparison.OrdinalIgnoreCase);

    private void HandleRotary(TourBoxEvent e, bool clockwise)
    {
        if (e.Keys.Length == 0) return;
        var key = e.Keys[0];

        if (!_config.Rotary.TryGetValue(key.ToString(), out var rotary))
            return;

        var binding = clockwise ? rotary.Clockwise : rotary.CounterClockwise;
        if (binding == null || string.IsNullOrWhiteSpace(binding.Action))
            return;

        ExecuteAction(binding, isDown: false);
    }

    private void ExecuteAction(KeyBinding config, bool? isDown)
    {
        var entry = MappingEntry.FromKeyBinding(config);

        switch (entry.Kind)
        {
            case ActionKind.MouseClick:
                if (isDown != false)
                {
                    if (entry.MouseButton == "Left") _input.Mouse.LeftButtonClick();
                    else if (entry.MouseButton == "Right") _input.Mouse.RightButtonClick();
                    else _input.Mouse.MiddleButtonClick();
                }
                return;

            case ActionKind.MouseScroll:
            {
                int val = entry.ScrollDirection is "Up" or "Right" ? 1 : -1;
                if (entry.ScrollDirection is "Left" or "Right")
                    _input.Mouse.HorizontalScroll(val * 2);
                else
                    _input.Mouse.VerticalScroll(val * 2);
                return;
            }

            case ActionKind.Text:
                if (isDown != true)
                    _input.Keyboard.TextEntry(entry.Text);
                return;
        }

        // Keyboard: parse combo
        var parts = entry.KeyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var keys = new List<VirtualKeyCode>();

        foreach (var part in parts)
        {
            var normalized = part.StartsWith("VK_", StringComparison.OrdinalIgnoreCase)
                ? part[3..]
                : part;

            if (Enum.TryParse<VirtualKeyCode>(normalized, true, out var code))
            {
                keys.Add(code);
            }
            else if (Enum.TryParse<VirtualKeyCode>(part, true, out var code2))
            {
                keys.Add(code2);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[InputService] Unknown key: {part}");
                return;
            }
        }

        if (keys.Count == 0) return;

        if (isDown.HasValue)
        {
            // Hold mode
            if (isDown.Value)
            {
                foreach (var k in keys) _input.Keyboard.KeyDown(k);
            }
            else
            {
                foreach (var k in Enumerable.Reverse(keys)) _input.Keyboard.KeyUp(k);
            }
        }
        else
        {
            // Tap mode
            if (keys.Count == 1)
            {
                _input.Keyboard.KeyPress(keys[0]);
            }
            else
            {
                var modifiers = keys.Take(keys.Count - 1);
                var mainKey = keys.Last();
                _input.Keyboard.ModifiedKeyStroke(modifiers, mainKey);
            }
        }
    }

    public void Dispose()
    {
        _tourBoxService.ButtonEvent -= OnTourBoxEvent;

        // Release any held keys
        lock (_lock)
        {
            foreach (var key in _pressedKeys)
            {
                if (_config.Keys.TryGetValue(key.ToString(), out var binding) && IsHoldMode(binding))
                    ExecuteAction(binding, isDown: false);
            }
            _pressedKeys.Clear();
            _consumedKeys.Clear();
        }
    }
}
