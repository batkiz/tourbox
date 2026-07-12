using System.Windows;
using kiwiprojekt.tourbox.ui.Models;

namespace kiwiprojekt.tourbox.ui.Views;

/// <summary>
/// Dialog for editing a single TourBox control mapping.
/// </summary>
public partial class MappingEditorDialog : Window
{
    public MappingEntry Result { get; private set; }

    public MappingEditorDialog(string controlLabel, MappingEntry entry, bool showMode = true)
    {
        InitializeComponent();

        var vm = new MappingEditorViewModel(controlLabel, entry, showMode);
        DataContext = vm;

        Result = entry;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MappingEditorViewModel vm)
        {
            Result = vm.ToMappingEntry();
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// ViewModel for the mapping editor dialog.
/// </summary>
public class MappingEditorViewModel : BindableBase
{
    private readonly bool _showMode;

    public string ControlLabel { get; }

    public string[] ActionTypes { get; } = { "Keyboard", "Mouse Click", "Mouse Scroll", "Text" };
    public string[] MouseButtons { get; } = { "Left", "Right", "Middle" };
    public string[] ScrollDirections { get; } = { "Up", "Down", "Left", "Right" };

    private string _actionType = "Keyboard";
    public string ActionType
    {
        get => _actionType;
        set
        {
            if (SetProperty(ref _actionType, value))
            {
                OnPropertyChanged(nameof(IsKeyboard));
                OnPropertyChanged(nameof(IsMouseClick));
                OnPropertyChanged(nameof(IsMouseScroll));
                OnPropertyChanged(nameof(IsText));
            }
        }
    }

    private string _keyCombo = "";
    public string KeyCombo { get => _keyCombo; set => SetProperty(ref _keyCombo, value); }

    private string _mouseButton = "Left";
    public string MouseButton { get => _mouseButton; set => SetProperty(ref _mouseButton, value); }

    private string _scrollDirection = "Up";
    public string ScrollDirection { get => _scrollDirection; set => SetProperty(ref _scrollDirection, value); }

    private string _text = "";
    public string Text { get => _text; set => SetProperty(ref _text, value); }

    private string _mode = "Tap";
    public string Mode { get => _mode; set => SetProperty(ref _mode, value); }

    public bool IsKeyboard => ActionType == "Keyboard";
    public bool IsMouseClick => ActionType == "Mouse Click";
    public bool IsMouseScroll => ActionType == "Mouse Scroll";
    public bool IsText => ActionType == "Text";

    public bool ShowMode => _showMode;

    public bool IsTapMode
    {
        get => Mode == "Tap";
        set { if (value) Mode = "Tap"; }
    }

    public bool IsHoldMode
    {
        get => Mode == "Hold";
        set { if (value) Mode = "Hold"; }
    }

    public MappingEditorViewModel(string controlLabel, MappingEntry entry, bool showMode)
    {
        ControlLabel = controlLabel;
        _showMode = showMode;

        ActionType = entry.ActionType;
        KeyCombo = entry.KeyCombo;
        MouseButton = entry.MouseButton;
        ScrollDirection = entry.ScrollDirection;
        Text = entry.Text;
        Mode = entry.Mode;
    }

    public MappingEntry ToMappingEntry()
    {
        return new MappingEntry
        {
            ActionType = ActionType,
            KeyCombo = KeyCombo,
            MouseButton = MouseButton,
            ScrollDirection = ScrollDirection,
            Text = Text,
            Mode = Mode
        };
    }
}
