namespace kiwiprojekt.tourbox.ui.Models;

/// <summary>
/// Represents a configurable action for a TourBox control.
/// </summary>
public class MappingEntry : BindableBase
{
    private string _actionType = "Keyboard";
    public string ActionType
    {
        get => _actionType;
        set { if (SetProperty(ref _actionType, value)) UpdatePreview(); }
    }

    private string _keyCombo = "";
    public string KeyCombo
    {
        get => _keyCombo;
        set { if (SetProperty(ref _keyCombo, value)) UpdatePreview(); }
    }

    private string _mouseButton = "Left";
    public string MouseButton
    {
        get => _mouseButton;
        set { if (SetProperty(ref _mouseButton, value)) UpdatePreview(); }
    }

    private string _scrollDirection = "Up";
    public string ScrollDirection
    {
        get => _scrollDirection;
        set { if (SetProperty(ref _scrollDirection, value)) UpdatePreview(); }
    }

    private string _text = "";
    public string Text
    {
        get => _text;
        set { if (SetProperty(ref _text, value)) UpdatePreview(); }
    }

    private string _mode = "Tap";
    public string Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    private string _preview = "";
    public string Preview
    {
        get => _preview;
        set => SetProperty(ref _preview, value);
    }

    public string[] ActionTypes { get; } = { "Keyboard", "Mouse Click", "Mouse Scroll", "Text" };
    public string[] MouseButtons { get; } = { "Left", "Right", "Middle" };
    public string[] ScrollDirections { get; } = { "Up", "Down", "Left", "Right" };
    public string[] Modes { get; } = { "Tap", "Hold" };

    public MappingEntry()
    {
        UpdatePreview();
    }

    /// <summary>
    /// Load from a KeyBinding config.
    /// </summary>
    public static MappingEntry FromKeyBinding(KeyBinding? binding)
    {
        var entry = new MappingEntry();
        if (binding == null || string.IsNullOrWhiteSpace(binding.Action))
            return entry;

        var action = binding.Action.Trim();

        if (action.Equals("LeftClick", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("RightClick", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("MiddleClick", StringComparison.OrdinalIgnoreCase))
        {
            entry.ActionType = "Mouse Click";
            entry.MouseButton = action.Replace("Click", "");
        }
        else if (action.Equals("VerticalScroll", StringComparison.OrdinalIgnoreCase) ||
                 action.Equals("HorizontalScroll", StringComparison.OrdinalIgnoreCase))
        {
            entry.ActionType = "Mouse Scroll";
            var dir = binding.Value?.Trim() ?? "Up";
            if (action.Equals("HorizontalScroll", StringComparison.OrdinalIgnoreCase))
                dir = dir == "Up" ? "Right" : dir == "Down" ? "Left" : dir;
            entry.ScrollDirection = dir;
        }
        else if (action.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
        {
            entry.ActionType = "Text";
            entry.Text = action["Text:".Length..];
        }
        else
        {
            entry.ActionType = "Keyboard";
            entry.KeyCombo = action;
        }

        entry.Mode = binding.Mode ?? "Tap";
        return entry;
    }

    /// <summary>
    /// Convert to a KeyBinding for saving.
    /// </summary>
    public KeyBinding ToKeyBinding()
    {
        var action = ActionType switch
        {
            "Mouse Click" => $"{MouseButton}Click",
            "Mouse Scroll" => ActionType == "Mouse Scroll"
                ? (ScrollDirection is "Left" or "Right" ? "HorizontalScroll" : "VerticalScroll")
                : "VerticalScroll",
            "Text" => $"Text:{Text}",
            _ => KeyCombo
        };

        var value = ActionType == "Mouse Scroll"
            ? (ScrollDirection is "Up" or "Down" ? ScrollDirection : ScrollDirection) 
            : "";

        return new KeyBinding { Action = action, Mode = Mode, Value = value };
    }

    private void UpdatePreview()
    {
        Preview = ActionType switch
        {
            "Keyboard" => string.IsNullOrWhiteSpace(KeyCombo) ? "(未设置)" : KeyCombo,
            "Mouse Click" => $"鼠标{GetChineseName(MouseButton)}键",
            "Mouse Scroll" => $"滚轮{GetChineseDirection(ScrollDirection)}",
            "Text" => string.IsNullOrWhiteSpace(Text) ? "(未设置)" : $"输入: {Truncate(Text, 20)}",
            _ => "(未设置)"
        };
    }

    private static string GetChineseName(string btn) => btn switch
    {
        "Left" => "左", "Right" => "右", "Middle" => "中", _ => btn
    };

    private static string GetChineseDirection(string dir) => dir switch
    {
        "Up" => "上滚", "Down" => "下滚", "Left" => "左滚", "Right" => "右滚", _ => dir
    };

    private static string Truncate(string s, int len) =>
        s.Length <= len ? s : s[..len] + "...";
}
