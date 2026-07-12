using kiwiprojekt.tourbox.ui.Models;

namespace kiwiprojekt.tourbox.ui.ViewModels;

/// <summary>
/// ViewModel for the inline control editor panel.
/// Holds a MappingEntry and a RotaryEntry for rotary controls.
/// </summary>
public class EditorViewModel : BindableBase
{
    private string _controlName = "";
    public string ControlName { get => _controlName; set => SetProperty(ref _controlName, value); }

    private bool _showMode = true;
    public bool ShowMode { get => _showMode; set => SetProperty(ref _showMode, value); }

    private bool _isRotary;
    public bool IsRotary { get => _isRotary; set => SetProperty(ref _isRotary, value); }

    // ── Single button / CW rotary ──
    private MappingEntry _entry = new();
    public MappingEntry Entry => _entry;

    // ── CCW rotary ──
    private MappingEntry _ccwEntry = new();
    public MappingEntry CcwEntry => _ccwEntry;

    // ── Visibility bindings (proxy to Entry.Kind) ──
    public bool IsKeyboard => _entry.Kind == ActionKind.Keyboard;
    public bool IsMouseClick => _entry.Kind == ActionKind.MouseClick;
    public bool IsMouseScroll => _entry.Kind == ActionKind.MouseScroll;
    public bool IsText => _entry.Kind == ActionKind.Text;

    public bool IsCwKeyboard => _entry.Kind == ActionKind.Keyboard;
    public bool IsCcwKeyboard => _ccwEntry.Kind == ActionKind.Keyboard;

    // ── ActionKind ↔ string for ComboBox binding ──
    public string ActionType
    {
        get => _entry.Kind.ToString();
        set { _entry.Kind = Enum.Parse<ActionKind>(value); RefreshKindFlags(); }
    }

    public string CwActionType
    {
        get => _entry.Kind.ToString();
        set { _entry.Kind = Enum.Parse<ActionKind>(value); RefreshKindFlags(); }
    }

    public string CcwActionType
    {
        get => _ccwEntry.Kind.ToString();
        set { _ccwEntry.Kind = Enum.Parse<ActionKind>(value); RefreshKindFlags(); }
    }

    // ── KeyCombo proxies (XAML binds to Editor.KeyCombo, not Editor.Entry.KeyCombo) ──
    public string KeyCombo { get => _entry.KeyCombo; set => _entry.KeyCombo = value; }
    // ── Mode binding (ComboBox uses strings) ──
    public string Mode
    {
        get => _entry.Mode == BindMode.Hold ? "Hold" : "Tap";
        set => _entry.Mode = value == "Hold" ? BindMode.Hold : BindMode.Tap;
    }
    public string MouseButton { get => _entry.MouseButton; set => _entry.MouseButton = value; }
    public string ScrollDirection { get => _entry.ScrollDirection; set => _entry.ScrollDirection = value; }
    public string Text { get => _entry.Text; set => _entry.Text = value; }

    public string CwKeyCombo { get => _entry.KeyCombo; set => _entry.KeyCombo = value; }
    public string CcwKeyCombo { get => _ccwEntry.KeyCombo; set => _ccwEntry.KeyCombo = value; }

    // ── Lists for ComboBoxes ──
    public string[] ActionTypes { get; } = { "Keyboard", "MouseClick", "MouseScroll", "Text" };
    public string[] MouseButtons { get; } = { "Left", "Right", "Middle" };
    public string[] ScrollDirections { get; } = { "Up", "Down", "Left", "Right" };
    public string[] Modes { get; } = { "Tap", "Hold" };

    private string _configKey = "";

    public EditorViewModel()
    {
        _entry.PropertyChanged += (_, _) => RefreshKindFlags();
        _ccwEntry.PropertyChanged += (_, _) => RefreshKindFlags();
    }

    private void RefreshKindFlags()
    {
        OnPropertyChanged(nameof(IsKeyboard));
        OnPropertyChanged(nameof(IsMouseClick));
        OnPropertyChanged(nameof(IsMouseScroll));
        OnPropertyChanged(nameof(IsText));
        OnPropertyChanged(nameof(IsCwKeyboard));
        OnPropertyChanged(nameof(IsCcwKeyboard));
    }

    public void LoadButton(string key, KeyBinding? binding)
    {
        _configKey = key;
        IsRotary = false;
        ShowMode = true;

        _entry = MappingEntry.FromKeyBinding(binding ?? new KeyBinding());
        ControlName = key;
        OnPropertyChanged(nameof(Entry));
        OnPropertyChanged(nameof(KeyCombo));
        OnPropertyChanged(nameof(Mode));
        RefreshKindFlags();
    }

    public void LoadRotary(string key, RotaryBinding? rotary)
    {
        _configKey = key;
        IsRotary = true;
        ShowMode = false;

        _entry = MappingEntry.FromKeyBinding(rotary?.Clockwise);
        _ccwEntry = MappingEntry.FromKeyBinding(rotary?.CounterClockwise);
        ControlName = key;
        OnPropertyChanged(nameof(Entry));
        OnPropertyChanged(nameof(CcwEntry));
        OnPropertyChanged(nameof(CwKeyCombo));
        OnPropertyChanged(nameof(CcwKeyCombo));
        RefreshKindFlags();
    }

    public void SaveTo(AppConfig config)
    {
        if (string.IsNullOrEmpty(_configKey)) return;

        if (IsRotary)
        {
            config.Rotary[_configKey] = new RotaryBinding
            {
                Clockwise = _entry.ToKeyBinding(),
                CounterClockwise = _ccwEntry.ToKeyBinding()
            };
        }
        else
        {
            config.Keys[_configKey] = _entry.ToKeyBinding();
        }
    }
}
