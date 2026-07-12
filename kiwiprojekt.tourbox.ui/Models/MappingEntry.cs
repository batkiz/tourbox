namespace kiwiprojekt.tourbox.ui.Models;

public enum ActionKind
{
    Keyboard,
    MouseClick,
    MouseScroll,
    Text
}

/// <summary>
/// Represents a configurable action, with serialization to/from KeyBinding.
/// Single source of truth for action string parsing.
/// </summary>
public class MappingEntry : BindableBase
{
    private ActionKind _kind = ActionKind.Keyboard;
    public ActionKind Kind
    {
        get => _kind;
        set { if (SetProperty(ref _kind, value)) UpdatePreview(); }
    }

    private string _keyCombo = "";
    public string KeyCombo { get => _keyCombo; set { if (SetProperty(ref _keyCombo, value)) UpdatePreview(); } }

    private string _mouseButton = "Left";
    public string MouseButton { get => _mouseButton; set { if (SetProperty(ref _mouseButton, value)) UpdatePreview(); } }

    private string _scrollDirection = "Up";
    public string ScrollDirection { get => _scrollDirection; set { if (SetProperty(ref _scrollDirection, value)) UpdatePreview(); } }

    private string _text = "";
    public string Text { get => _text; set { if (SetProperty(ref _text, value)) UpdatePreview(); } }

    private string _mode = "Tap";
    public string Mode { get => _mode; set => SetProperty(ref _mode, value); }

    private string _preview = "";
    public string Preview { get => _preview; private set => SetProperty(ref _preview, value); }

    public string[] Modes { get; } = { "Tap", "Hold" };

    public MappingEntry()
    {
        UpdatePreview();
    }

    /// <summary>
    /// Deserialize from config binding.
    /// </summary>
    public static MappingEntry FromKeyBinding(KeyBinding? binding)
    {
        var entry = new MappingEntry();
        if (binding == null || string.IsNullOrWhiteSpace(binding.Action))
            return entry;

        var action = binding.Action.Trim();

        if (action is "LeftClick" or "RightClick" or "MiddleClick")
        {
            entry.Kind = ActionKind.MouseClick;
            entry.MouseButton = action.Replace("Click", "");
        }
        else if (action is "VerticalScroll" or "HorizontalScroll")
        {
            entry.Kind = ActionKind.MouseScroll;
            var dir = binding.Value?.Trim() ?? "Up";
            if (action == "HorizontalScroll")
                dir = dir is "Up" ? "Right" : dir is "Down" ? "Left" : dir;
            entry.ScrollDirection = dir;
        }
        else if (action.StartsWith("Text:", StringComparison.OrdinalIgnoreCase))
        {
            entry.Kind = ActionKind.Text;
            entry.Text = action["Text:".Length..];
        }
        else
        {
            entry.Kind = ActionKind.Keyboard;
            entry.KeyCombo = action;
        }

        entry.Mode = binding.Mode ?? "Tap";
        return entry;
    }

    /// <summary>
    /// Serialize to config binding.
    /// </summary>
    public KeyBinding ToKeyBinding()
    {
        var action = Kind switch
        {
            ActionKind.MouseClick => $"{MouseButton}Click",
            ActionKind.MouseScroll => ScrollDirection is "Left" or "Right"
                ? "HorizontalScroll" : "VerticalScroll",
            ActionKind.Text => $"Text:{Text}",
            _ => KeyCombo
        };

        var value = Kind == ActionKind.MouseScroll ? ScrollDirection : "";

        return new KeyBinding { Action = action, Mode = Mode, Value = value };
    }

    private void UpdatePreview()
    {
        Preview = Kind switch
        {
            ActionKind.Keyboard => string.IsNullOrWhiteSpace(KeyCombo) ? "(未设置)" : KeyCombo,
            ActionKind.MouseClick => $"鼠标{MouseButton switch { "Left" => "左", "Right" => "右", _ => "中" }}键",
            ActionKind.MouseScroll => $"滚轮{ScrollDirection switch { "Up" => "上", "Down" => "下", "Left" => "左", "Right" => "右", _ => "" }}",
            ActionKind.Text => string.IsNullOrWhiteSpace(Text) ? "(未设置)" : $"输入: {Truncate(Text, 20)}",
            _ => "(未设置)"
        };
    }

    private static string Truncate(string s, int len) =>
        s.Length <= len ? s : s[..len] + "...";
}
