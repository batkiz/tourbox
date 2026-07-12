using kiwiprojekt.tourbox.ui.Models;

namespace kiwiprojekt.tourbox.ui.ViewModels;

/// <summary>
/// ViewModel for the inline control editor panel.
/// </summary>
public class EditorViewModel : BindableBase
{
    // ── Common ──
    private string _controlName = "";
    public string ControlName { get => _controlName; set => SetProperty(ref _controlName, value); }

    private string _actionType = "Keyboard";
    public string ActionType
    {
        get => _actionType;
        set { if (SetProperty(ref _actionType, value)) RefreshVisibility(); }
    }

    private string _mode = "Tap";
    public string Mode { get => _mode; set => SetProperty(ref _mode, value); }

    public string[] ActionTypes { get; } = { "Keyboard", "Mouse Click", "Mouse Scroll", "Text" };
    public string[] MouseButtons { get; } = { "Left", "Right", "Middle" };
    public string[] ScrollDirections { get; } = { "Up", "Down", "Left", "Right" };
    public string[] Modes { get; } = { "Tap", "Hold" };

    private bool _showMode = true;
    public bool ShowMode { get => _showMode; set => SetProperty(ref _showMode, value); }

    private bool _isRotary;
    public bool IsRotary { get => _isRotary; set => SetProperty(ref _isRotary, value); }

    // ── Keyboard ──
    private string _keyCombo = "";
    public string KeyCombo { get => _keyCombo; set => SetProperty(ref _keyCombo, value); }

    // ── Mouse ──
    private string _mouseButton = "Left";
    public string MouseButton { get => _mouseButton; set => SetProperty(ref _mouseButton, value); }

    private string _scrollDirection = "Up";
    public string ScrollDirection { get => _scrollDirection; set => SetProperty(ref _scrollDirection, value); }

    // ── Text ──
    private string _text = "";
    public string Text { get => _text; set => SetProperty(ref _text, value); }

    // ── Visibility flags ──
    private bool _isKeyboard = true;
    public bool IsKeyboard { get => _isKeyboard; set => SetProperty(ref _isKeyboard, value); }

    private bool _isMouseClick;
    public bool IsMouseClick { get => _isMouseClick; set => SetProperty(ref _isMouseClick, value); }

    private bool _isMouseScroll;
    public bool IsMouseScroll { get => _isMouseScroll; set => SetProperty(ref _isMouseScroll, value); }

    private bool _isText;
    public bool IsText { get => _isText; set => SetProperty(ref _isText, value); }

    // ── Rotary: CW ──
    private string _cwActionType = "Keyboard";
    public string CwActionType
    {
        get => _cwActionType;
        set { if (SetProperty(ref _cwActionType, value)) RefreshRotaryVisibility(); }
    }

    private string _cwKeyCombo = "";
    public string CwKeyCombo { get => _cwKeyCombo; set => SetProperty(ref _cwKeyCombo, value); }

    private string _cwMouseButton = "Left";
    public string CwMouseButton { get => _cwMouseButton; set => SetProperty(ref _cwMouseButton, value); }

    private string _cwScrollDir = "Up";
    public string CwScrollDir { get => _cwScrollDir; set => SetProperty(ref _cwScrollDir, value); }

    private string _cwText = "";
    public string CwText { get => _cwText; set => SetProperty(ref _cwText, value); }

    private bool _isCwKeyboard = true;
    public bool IsCwKeyboard { get => _isCwKeyboard; set => SetProperty(ref _isCwKeyboard, value); }

    // ── Rotary: CCW ──
    private string _ccwActionType = "Keyboard";
    public string CcwActionType
    {
        get => _ccwActionType;
        set { if (SetProperty(ref _ccwActionType, value)) RefreshRotaryVisibility(); }
    }

    private string _ccwKeyCombo = "";
    public string CcwKeyCombo { get => _ccwKeyCombo; set => SetProperty(ref _ccwKeyCombo, value); }

    private string _ccwMouseButton = "Left";
    public string CcwMouseButton { get => _ccwMouseButton; set => SetProperty(ref _ccwMouseButton, value); }

    private string _ccwScrollDir = "Up";
    public string CcwScrollDir { get => _ccwScrollDir; set => SetProperty(ref _ccwScrollDir, value); }

    private string _ccwText = "";
    public string CcwText { get => _ccwText; set => SetProperty(ref _ccwText, value); }

    private bool _isCcwKeyboard = true;
    public bool IsCcwKeyboard { get => _isCcwKeyboard; set => SetProperty(ref _isCcwKeyboard, value); }

    private string _configKey = "";

    private void RefreshVisibility()
    {
        IsKeyboard = ActionType == "Keyboard";
        IsMouseClick = ActionType == "Mouse Click";
        IsMouseScroll = ActionType == "Mouse Scroll";
        IsText = ActionType == "Text";
    }

    private void RefreshRotaryVisibility()
    {
        IsCwKeyboard = CwActionType == "Keyboard";
        IsCcwKeyboard = CcwActionType == "Keyboard";
    }

    public void LoadButton(string key, KeyBinding? binding)
    {
        _configKey = key;
        IsRotary = false;
        ShowMode = true;

        var entry = MappingEntry.FromKeyBinding(binding ?? new KeyBinding());
        ControlName = key;
        ActionType = entry.ActionType;
        Mode = entry.Mode;
        KeyCombo = entry.KeyCombo;
        MouseButton = entry.MouseButton;
        ScrollDirection = entry.ScrollDirection;
        Text = entry.Text;
        RefreshVisibility();
    }

    public void LoadRotary(string key, RotaryBinding? rotary)
    {
        _configKey = key;
        IsRotary = true;
        ShowMode = false;

        var cw = MappingEntry.FromKeyBinding(rotary?.Clockwise);
        var ccw = MappingEntry.FromKeyBinding(rotary?.CounterClockwise);

        ControlName = key;
        CwActionType = cw.ActionType;
        CwKeyCombo = cw.KeyCombo;
        CcwActionType = ccw.ActionType;
        CcwKeyCombo = ccw.KeyCombo;
        RefreshRotaryVisibility();
    }

    public void SaveTo(AppConfig config)
    {
        if (string.IsNullOrEmpty(_configKey)) return;

        if (IsRotary)
        {
            config.Rotary[_configKey] = new RotaryBinding
            {
                Clockwise = BuildKeyBinding(CwActionType, CwKeyCombo, CwMouseButton, CwScrollDir, CwText),
                CounterClockwise = BuildKeyBinding(CcwActionType, CcwKeyCombo, CcwMouseButton, CcwScrollDir, CcwText)
            };
        }
        else
        {
            config.Keys[_configKey] = BuildKeyBinding(ActionType, KeyCombo, MouseButton, ScrollDirection, Text, Mode);
        }
    }

    private static KeyBinding BuildKeyBinding(string type, string combo, string mouse, string scroll, string text, string mode = "Tap")
    {
        var (action, value) = type switch
        {
            "Mouse Click" => ($"{mouse}Click", ""),
            "Mouse Scroll" => (scroll is "Left" or "Right" ? "HorizontalScroll" : "VerticalScroll", scroll),
            "Text" => ($"Text:{text}", ""),
            _ => (combo, "")
        };
        return new KeyBinding { Action = action, Mode = mode, Value = value };
    }
}
