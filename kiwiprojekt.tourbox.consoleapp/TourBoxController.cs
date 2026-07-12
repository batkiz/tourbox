using WindowsInput;

namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxController
    {
        private const string HoldMode = "Hold";
        private const string TapMode = "Tap";

        private readonly TourBoxConfig _config;
        private readonly IInputSimulator _input;
        private readonly Dictionary<TourBoxKey, KeyConfig> _keyBindings;
        private readonly List<ComboBinding> _comboBindings;
        private readonly Dictionary<TourBoxKey, RotaryConfig> _rotaryBindings;
        private readonly HashSet<TourBoxKey> _pressedKeys = new();
        private readonly HashSet<TourBoxKey> _consumedKeys = new();
        private readonly object _lock = new();

        public TourBoxController(TourBoxConfig config, IInputSimulator input)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(input);

            _config = config;
            _input = input;
            _keyBindings = BuildKeyBindings(config.Keys);
            _comboBindings = BuildComboBindings(config.Combos);
            _rotaryBindings = BuildRotaryBindings(config.Rotary);
        }

        public void HandleEvent(TourBoxEvent e)
        {
            ArgumentNullException.ThrowIfNull(e);

            lock (_lock)
            {
                if (_config.DebugMode)
                {
                    FileLogger.Log($"[DEBUG] Event Received: {e}");
                }

                try 
                {
                    switch (e.Action)
                    {
                        case ActionType.Increased when TryGetPrimaryKey(e, out var increasedKey):
                            HandleRotary(increasedKey, true);
                            break;
                        case ActionType.Decreased when TryGetPrimaryKey(e, out var decreasedKey):
                            HandleRotary(decreasedKey, false);
                            break;
                        case ActionType.Click when TryGetPrimaryKey(e, out var pressedKey):
                            OnKeyDown(pressedKey);
                            break;
                        case ActionType.ClickReleased when TryGetPrimaryKey(e, out var releasedKey):
                            OnKeyUp(releasedKey);
                            break;
                        default:
                            if (_config.DebugMode)
                            {
                                FileLogger.Log($"[DEBUG] Ignored event: {e}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Error handling event {e}: {ex}");
                }
            }
        }

        private void HandleRotary(TourBoxKey key, bool clockwise)
        {
            if (_rotaryBindings.TryGetValue(key, out var rotaryConfig))
            {
                var action = clockwise ? rotaryConfig.Clockwise : rotaryConfig.CounterClockwise;
                ExecuteAction(action);
            }
        }

        private void OnKeyDown(TourBoxKey key)
        {
            _pressedKeys.Add(key);

            // 1. Check for Combos
            // Construct potential combos from pressed keys
            // Simple approach: Check all configured combos
            foreach (var combo in _comboBindings)
            {
                // Check if all keys in this combo are pressed
                if (combo.Keys.All(k => _pressedKeys.Contains(k)))
                {
                    // Check if the current key is part of this combo (it triggered it)
                    if (combo.Keys.Contains(key))
                    {
                        ExecuteAction(combo.Config);
                        // Mark all involved keys as consumed
                        foreach (var comboKey in combo.Keys)
                        {
                            _consumedKeys.Add(comboKey);
                        }

                        return; // Combo executed, skip single action
                    }
                }
            }

            // 2. Check Single Key Action
            if (_consumedKeys.Contains(key)) return;

            if (_keyBindings.TryGetValue(key, out var config))
            {
                if (IsHoldMode(config))
                {
                    ExecuteAction(config, isDown: true);
                }
                // If Tap, we wait for Release
            }
        }

        private void OnKeyUp(TourBoxKey key)
        {
            _pressedKeys.Remove(key);

            if (_consumedKeys.Contains(key))
            {
                _consumedKeys.Remove(key);
                // If it was a Hold key that participated in a combo, we might want to release it?
                // But for now, assume Combo consumed it entirely. 
                // However, if we mapped "Tall" -> "Ctrl" (Hold), and "Tall+Top" -> "Copy".
                // Press Tall (Ctrl Down). Press Top (Copy). Release Top. Release Tall.
                // We need to ensure Ctrl comes UP.
                // Re-checking config for "Hold" mode cleanup:
                if (_keyBindings.TryGetValue(key, out var config))
                {
                    if (IsHoldMode(config))
                    {
                        ExecuteAction(config, isDown: false);
                    }
                }

                return;
            }

            // Single Tap Action
            if (_keyBindings.TryGetValue(key, out var keyConfig))
            {
                if (IsTapMode(keyConfig))
                {
                    ExecuteAction(keyConfig);
                }
                else if (IsHoldMode(keyConfig))
                {
                    ExecuteAction(keyConfig, isDown: false);
                }
            }
        }

        private void ExecuteAction(KeyConfig config, bool? isDown = null)
        {
            if (string.IsNullOrWhiteSpace(config.Action))
            {
                return;
            }

            var action = config.Action.Trim();

            // Log for debug
            if (_config.DebugMode)
            {
                FileLogger.Log($"Executing: {action} (Mode: {config.Mode}, Val: {config.Value})");
            }

            // 1. Special Commands (Mouse, etc.)
            // Check these first to avoid parsing issues or conflicts
            if (action.Equals("LeftClick", StringComparison.OrdinalIgnoreCase))
            {
                _input.Mouse.LeftButtonClick();
                return;
            }

            if (action.Equals("RightClick", StringComparison.OrdinalIgnoreCase))
            {
                _input.Mouse.RightButtonClick();
                return;
            }

            if (action.Equals("MiddleClick", StringComparison.OrdinalIgnoreCase))
            {
                _input.Mouse.MiddleButtonClick();
                return;
            }

            if (action.Equals("VerticalScroll", StringComparison.OrdinalIgnoreCase))
            {
                var val = string.Equals(config.Value, "Up", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
                _input.Mouse.VerticalScroll(val * 2);
                return;
            }

            if (action.Equals("HorizontalScroll", StringComparison.OrdinalIgnoreCase))
            {
                var hVal = string.Equals(config.Value, "Right", StringComparison.OrdinalIgnoreCase) ? 1 : -1;
                _input.Mouse.HorizontalScroll(hVal * 2);
                return;
            }

            // 2. Text Entry
            if (action.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
            {
                if (isDown != true) // Execute on Tap or Release (Standard for macros)
                {
                    var text = action["Text:".Length..];
                    _input.Keyboard.TextEntry(text);
                }
                return;
            }

            // 3. Key Parsing (Handles Single Keys and Combos like "CONTROL+C")
            var parts = action.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var keys = new List<VirtualKeyCode>();

            foreach (var part in parts)
            {
                // Normalize: "VK_BROWSER_BACK" -> "BROWSER_BACK"
                var normalizedPart = part.StartsWith("VK_", StringComparison.OrdinalIgnoreCase) 
                    ? part.Substring(3) 
                    : part;

                if (Enum.TryParse<VirtualKeyCode>(normalizedPart, true, out var code))
                {
                    keys.Add(code);
                }
                else
                {
                    // Fallback: Try original just in case input simulator changes enum names?
                    // But usually stripping VK_ works for standard Win32 names mapped to InputSimulator.
                    if (Enum.TryParse<VirtualKeyCode>(part, true, out var code2))
                    {
                         keys.Add(code2);
                    }
                    else
                    {
                        FileLogger.Log($"[WARNING] Unknown key in action '{action}': {part}");
                        return; // Abort if invalid key
                    }
                }
            }

            if (keys.Count == 0) return;

            // 4. Execution Logic
            if (isDown.HasValue) // Hold Mode (KeyDown/KeyUp)
            {
                // For Hold, we press/release ALL keys in the sequence?
                // Usually "Hold Ctrl+C" doesn't make sense. "Hold Ctrl" does.
                // But "Hold Ctrl+Shift" does.
                // Simple logic: Press all on Down, Release all in Reverse on Up.
                if (isDown.Value)
                {
                    foreach (var key in keys) _input.Keyboard.KeyDown(key);
                }
                else
                {
                    foreach (var key in Enumerable.Reverse(keys)) _input.Keyboard.KeyUp(key);
                }
            }
            else // Tap Mode (Press)
            {
                if (keys.Count == 1)
                {
                    _input.Keyboard.KeyPress(keys[0]);
                }
                else
                {
                    // Combo: Assume last is the trigger, others are modifiers
                    // e.g. "CONTROL+C" -> Modifiers: [CONTROL], Key: C
                    var modifiers = keys.Take(keys.Count - 1);
                    var key = keys.Last();
                    _input.Keyboard.ModifiedKeyStroke(modifiers, key);
                }
            }
        }

        private static bool TryGetPrimaryKey(TourBoxEvent e, out TourBoxKey key)
        {
            if (e.Keys.Length > 0)
            {
                key = e.Keys[0];
                return true;
            }

            key = default;
            return false;
        }

        private Dictionary<TourBoxKey, KeyConfig> BuildKeyBindings(Dictionary<string, KeyConfig> bindings)
        {
            var result = new Dictionary<TourBoxKey, KeyConfig>();

            foreach (var (bindingKey, config) in bindings)
            {
                if (!TryParseTourBoxKey(bindingKey, out var key))
                {
                    FileLogger.Log($"[WARNING] Ignoring invalid key binding '{bindingKey}'.");
                    continue;
                }

                result[key] = config;
            }

            return result;
        }

        private List<ComboBinding> BuildComboBindings(Dictionary<string, KeyConfig> bindings)
        {
            var result = new List<ComboBinding>();

            foreach (var (comboKey, config) in bindings)
            {
                var keys = new List<TourBoxKey>();
                var isValid = true;

                foreach (var rawKey in comboKey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!TryParseTourBoxKey(rawKey, out var key))
                    {
                        FileLogger.Log($"[WARNING] Ignoring invalid combo binding '{comboKey}'.");
                        isValid = false;
                        break;
                    }

                    keys.Add(key);
                }

                if (!isValid || keys.Count == 0)
                {
                    continue;
                }

                result.Add(new ComboBinding(keys.Distinct().ToArray(), config));
            }

            result.Sort(static (left, right) => right.Keys.Length.CompareTo(left.Keys.Length));
            return result;
        }

        private Dictionary<TourBoxKey, RotaryConfig> BuildRotaryBindings(Dictionary<string, RotaryConfig> bindings)
        {
            var result = new Dictionary<TourBoxKey, RotaryConfig>();

            foreach (var (bindingKey, config) in bindings)
            {
                if (!TryParseTourBoxKey(bindingKey, out var key))
                {
                    FileLogger.Log($"[WARNING] Ignoring invalid rotary binding '{bindingKey}'.");
                    continue;
                }

                result[key] = config;
            }

            return result;
        }

        private static bool TryParseTourBoxKey(string? value, out TourBoxKey key)
        {
            return Enum.TryParse(value, true, out key);
        }

        private static bool IsHoldMode(KeyConfig config)
        {
            return string.Equals(config.Mode, HoldMode, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTapMode(KeyConfig config)
        {
            return string.IsNullOrWhiteSpace(config.Mode) || string.Equals(config.Mode, TapMode, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record ComboBinding(TourBoxKey[] Keys, KeyConfig Config);
    }
}
