using WindowsInput;

namespace kiwiprojekt.tourbox.consoleapp
{
    public class TourBoxController
    {
        private readonly TourBoxConfig _config;
        private readonly IInputSimulator _input;
        private readonly HashSet<TourBoxKey> _pressedKeys = new();
        private readonly HashSet<TourBoxKey> _consumedKeys = new();
        private readonly object _lock = new();

        public TourBoxController(TourBoxConfig config, IInputSimulator input)
        {
            _config = config;
            _input = input;
        }

        public void HandleEvent(TourBoxEvent e)
        {
            lock (_lock)
            {
                if (_config.DebugMode)
                {
                    FileLogger.Log($"[DEBUG] Event Received: {e}");
                }

                try 
                {
                    if (e.Action == ActionType.Increased)
                        HandleRotary(e.Keys[0], true);
                    else if (e.Action == ActionType.Decreased)
                        HandleRotary(e.Keys[0], false);
                    else if (e.Action == ActionType.Click)
                        OnKeyDown(e.Keys[0]);
                    else if (e.Action == ActionType.ClickReleased)
                        OnKeyUp(e.Keys[0]);
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Error handling event {e}: {ex.Message}");
                }
            }
        }

        private void HandleRotary(TourBoxKey key, bool clockwise)
        {
            if (_config.Rotary.TryGetValue(key.ToString(), out var rotaryConfig))
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
            foreach (var combo in _config.Combos)
            {
                var keys = combo.Key.Split('+').Select(k => ParseTourBoxKey(k.Trim())).ToList();
                // Check if all keys in this combo are pressed
                if (keys.All(k => _pressedKeys.Contains(k)))
                {
                    // Check if the current key is part of this combo (it triggered it)
                    if (keys.Contains(key))
                    {
                        ExecuteAction(combo.Value);
                        // Mark all involved keys as consumed
                        foreach (var k in keys) _consumedKeys.Add(k);
                        return; // Combo executed, skip single action
                    }
                }
            }

            // 2. Check Single Key Action
            if (_consumedKeys.Contains(key)) return;

            if (_config.Keys.TryGetValue(key.ToString(), out var config))
            {
                if (config.Mode.Equals("Hold", StringComparison.OrdinalIgnoreCase))
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
                if (_config.Keys.TryGetValue(key.ToString(), out var config))
                {
                   if (config.Mode.Equals("Hold", StringComparison.OrdinalIgnoreCase))
                   {
                       ExecuteAction(config, isDown: false);
                   }
                }
                return;
            }

            // Single Tap Action
            if (_config.Keys.TryGetValue(key.ToString(), out var keyConfig))
            {
                if (keyConfig.Mode.Equals("Tap", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteAction(keyConfig);
                }
                else if (keyConfig.Mode.Equals("Hold", StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteAction(keyConfig, isDown: false);
                }
            }
        }

        private void ExecuteAction(KeyConfig config, bool? isDown = null)
        {
            if (string.IsNullOrEmpty(config.Action)) return;

            // Log for debug
            if (_config.DebugMode) FileLogger.Log($"Executing: {config.Action} (Mode: {config.Mode}, Val: {config.Value})");

            // 1. Special Commands (Mouse, etc.)
            // Check these first to avoid parsing issues or conflicts
            switch (config.Action.ToLower())
            {
                case "leftclick": _input.Mouse.LeftButtonClick(); return;
                case "rightclick": _input.Mouse.RightButtonClick(); return;
                case "middleclick": _input.Mouse.MiddleButtonClick(); return;
                case "verticalscroll":
                    int val = config.Value == "Up" ? 1 : -1;
                    _input.Mouse.VerticalScroll(val * 2);
                    return;
                case "horizontalscroll":
                    int hVal = config.Value == "Right" ? 1 : -1;
                    _input.Mouse.HorizontalScroll(hVal * 2);
                    return;
            }

            // 2. Text Entry
            if (config.Action.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
            {
                if (isDown != true) // Execute on Tap or Release (Standard for macros)
                {
                    var text = config.Action.Substring(5);
                    _input.Keyboard.TextEntry(text);
                }
                return;
            }

            // 3. Key Parsing (Handles Single Keys and Combos like "CONTROL+C")
            var parts = config.Action.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
                        if (_config.DebugMode) FileLogger.Log($"[WARNING] Unknown key: {part}");
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

        private TourBoxKey ParseTourBoxKey(string s)
        {
            return Enum.TryParse<TourBoxKey>(s, true, out var k) ? k : TourBoxKey.Side; // Default/Fallback
        }
    }
}
