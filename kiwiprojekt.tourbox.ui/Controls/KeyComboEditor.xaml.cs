using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace kiwiprojekt.tourbox.ui.Controls;

/// <summary>
/// Visual key combo editor with modifier checkboxes, key dropdown, and keyboard recording.
/// </summary>
public partial class KeyComboEditor : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty KeyComboProperty =
        DependencyProperty.Register(nameof(KeyCombo), typeof(string), typeof(KeyComboEditor),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnKeyComboChanged));

    public static readonly DependencyProperty MainKeyProperty =
        DependencyProperty.Register(nameof(MainKey), typeof(string), typeof(KeyComboEditor),
            new PropertyMetadata(""));

    public string KeyCombo
    {
        get => (string)GetValue(KeyComboProperty);
        set => SetValue(KeyComboProperty, value);
    }

    public string MainKey
    {
        get => (string)GetValue(MainKeyProperty);
        set => SetValue(MainKeyProperty, value);
    }

    private bool _isRecording;
    private bool _updating;

    public KeyComboEditor()
    {
        InitializeComponent();
    }

    private static void OnKeyComboChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyComboEditor editor && !editor._updating)
        {
            editor.ParseCombo((string)e.NewValue);
        }
    }

    /// <summary>
    /// Parse a "VK_MODIFIER+VK_MODIFIER+VK_KEY" string into UI state.
    /// </summary>
    private void ParseCombo(string combo)
    {
        _updating = true;

        ChkCtrl.IsChecked = false;
        ChkShift.IsChecked = false;
        ChkAlt.IsChecked = false;
        ChkWin.IsChecked = false;
        MainKey = "";

        if (string.IsNullOrWhiteSpace(combo))
        {
            _updating = false;
            UpdatePreview();
            return;
        }

        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var mainParts = new List<string>();

        foreach (var part in parts)
        {
            var normalized = part.StartsWith("VK_", StringComparison.OrdinalIgnoreCase)
                ? part[3..] : part;

            switch (normalized.ToUpperInvariant())
            {
                case "CONTROL": ChkCtrl.IsChecked = true; break;
                case "SHIFT": ChkShift.IsChecked = true; break;
                case "MENU": ChkAlt.IsChecked = true; break;
                case "LWIN":
                case "RWIN": ChkWin.IsChecked = true; break;
                default: mainParts.Add(normalized); break;
            }
        }

        if (mainParts.Count == 1)
            MainKey = mainParts[0];
        else if (mainParts.Count > 1)
            MainKey = string.Join("+", mainParts);

        _updating = false;
        UpdatePreview();
    }

    /// <summary>
    /// Build the "VK_X+VK_Y" string from current UI state.
    /// </summary>
    private string BuildCombo()
    {
        var parts = new List<string>();

        if (ChkCtrl.IsChecked == true) parts.Add("VK_CONTROL");
        if (ChkShift.IsChecked == true) parts.Add("VK_SHIFT");
        if (ChkAlt.IsChecked == true) parts.Add("VK_MENU");
        if (ChkWin.IsChecked == true) parts.Add("VK_LWIN");

        var main = MainKey?.Trim() ?? "";
        if (!string.IsNullOrEmpty(main))
        {
            // Handle multi-key main (shouldn't normally happen, but be safe)
            foreach (var k in main.Split('+', StringSplitOptions.TrimEntries))
            {
                var key = k.Trim();
                if (string.IsNullOrEmpty(key)) continue;
                parts.Add(key.StartsWith("VK_", StringComparison.OrdinalIgnoreCase) ? key : $"VK_{key}");
            }
        }

        return string.Join("+", parts);
    }

    private void Modifier_Changed(object sender, RoutedEventArgs e)
    {
        if (_updating) return;
        SyncToProperty();
    }

    private void CboKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        if (CboKey.SelectedItem is ComboBoxItem item)
            MainKey = item.Content?.ToString() ?? "";
        SyncToProperty();
    }

    private void CboKey_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            SyncToProperty();
            // Move focus away from combobox
            var parent = this.Parent as UIElement;
            parent?.Focus();
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        _updating = true;
        ChkCtrl.IsChecked = false;
        ChkShift.IsChecked = false;
        ChkAlt.IsChecked = false;
        ChkWin.IsChecked = false;
        MainKey = "";
        _updating = false;
        SyncToProperty();
    }

    private void BtnRecord_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            StopRecording();
            return;
        }

        StartRecording();
    }

    private void StartRecording()
    {
        _isRecording = true;
        BtnRecord.Content = "■ 停止";
        BtnRecord.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xE0, 0x40, 0x40));
        BtnRecord.ToolTip = "按下键盘组合键后自动捕获";

        // Preview hint
        RunPreview.Text = "请按键...";

        // Use WPF KeyDown at window level
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.KeyDown += OnRecordKeyDown;
            window.KeyUp += OnRecordKeyUp;
        }
    }

    private void StopRecording()
    {
        _isRecording = false;
        BtnRecord.Content = "● 录制";
        BtnRecord.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xFF, 0x57, 0x22));
        BtnRecord.ToolTip = "点击后按下键盘按键来捕获";

        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.KeyDown -= OnRecordKeyDown;
            window.KeyUp -= OnRecordKeyUp;
        }
    }

    private void OnRecordKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

        // Track modifiers
        if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl)
        {
            ChkCtrl.IsChecked = true;
        }
        else if (key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift)
        {
            ChkShift.IsChecked = true;
        }
        else if (key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt)
        {
            ChkAlt.IsChecked = true;
        }
        else if (key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
        {
            ChkWin.IsChecked = true;
        }
        else
        {
            // Main key
            var vkName = WpfKeyToVkName(key);
            if (!string.IsNullOrEmpty(vkName))
            {
                MainKey = vkName;
                StopRecording();
                SyncToProperty();
            }
        }
    }

    private void OnRecordKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Don't stop on key up - stay recording until a non-modifier key is pressed
        e.Handled = true;
    }

    private static string? WpfKeyToVkName(System.Windows.Input.Key key)
    {
        return key switch
        {
            System.Windows.Input.Key.A => "A",
            System.Windows.Input.Key.B => "B",
            System.Windows.Input.Key.C => "C",
            System.Windows.Input.Key.D => "D",
            System.Windows.Input.Key.E => "E",
            System.Windows.Input.Key.F => "F",
            System.Windows.Input.Key.G => "G",
            System.Windows.Input.Key.H => "H",
            System.Windows.Input.Key.I => "I",
            System.Windows.Input.Key.J => "J",
            System.Windows.Input.Key.K => "K",
            System.Windows.Input.Key.L => "L",
            System.Windows.Input.Key.M => "M",
            System.Windows.Input.Key.N => "N",
            System.Windows.Input.Key.O => "O",
            System.Windows.Input.Key.P => "P",
            System.Windows.Input.Key.Q => "Q",
            System.Windows.Input.Key.R => "R",
            System.Windows.Input.Key.S => "S",
            System.Windows.Input.Key.T => "T",
            System.Windows.Input.Key.U => "U",
            System.Windows.Input.Key.V => "V",
            System.Windows.Input.Key.W => "W",
            System.Windows.Input.Key.X => "X",
            System.Windows.Input.Key.Y => "Y",
            System.Windows.Input.Key.Z => "Z",
            System.Windows.Input.Key.D0 => "0",
            System.Windows.Input.Key.D1 => "1",
            System.Windows.Input.Key.D2 => "2",
            System.Windows.Input.Key.D3 => "3",
            System.Windows.Input.Key.D4 => "4",
            System.Windows.Input.Key.D5 => "5",
            System.Windows.Input.Key.D6 => "6",
            System.Windows.Input.Key.D7 => "7",
            System.Windows.Input.Key.D8 => "8",
            System.Windows.Input.Key.D9 => "9",
            System.Windows.Input.Key.F1 => "F1",
            System.Windows.Input.Key.F2 => "F2",
            System.Windows.Input.Key.F3 => "F3",
            System.Windows.Input.Key.F4 => "F4",
            System.Windows.Input.Key.F5 => "F5",
            System.Windows.Input.Key.F6 => "F6",
            System.Windows.Input.Key.F7 => "F7",
            System.Windows.Input.Key.F8 => "F8",
            System.Windows.Input.Key.F9 => "F9",
            System.Windows.Input.Key.F10 => "F10",
            System.Windows.Input.Key.F11 => "F11",
            System.Windows.Input.Key.F12 => "F12",
            System.Windows.Input.Key.Space => "SPACE",
            System.Windows.Input.Key.Tab => "TAB",
            System.Windows.Input.Key.Enter => "RETURN",
            System.Windows.Input.Key.Back => "BACK",
            System.Windows.Input.Key.Delete => "DELETE",
            System.Windows.Input.Key.Insert => "INSERT",
            System.Windows.Input.Key.Home => "HOME",
            System.Windows.Input.Key.End => "END",
            System.Windows.Input.Key.PageUp => "PAGEUP",
            System.Windows.Input.Key.PageDown => "PAGEDOWN",
            System.Windows.Input.Key.Up => "UP",
            System.Windows.Input.Key.Down => "DOWN",
            System.Windows.Input.Key.Left => "LEFT",
            System.Windows.Input.Key.Right => "RIGHT",
            System.Windows.Input.Key.Escape => "ESCAPE",
            System.Windows.Input.Key.PrintScreen => "PRINT",
            System.Windows.Input.Key.Scroll => "SCROLL",
            System.Windows.Input.Key.Pause => "PAUSE",
            System.Windows.Input.Key.CapsLock => "CAPITAL",
            System.Windows.Input.Key.NumLock => "NUMLOCK",
            System.Windows.Input.Key.BrowserBack => "BROWSER_BACK",
            System.Windows.Input.Key.BrowserForward => "BROWSER_FORWARD",
            System.Windows.Input.Key.BrowserRefresh => "BROWSER_REFRESH",
            System.Windows.Input.Key.BrowserHome => "BROWSER_HOME",
            System.Windows.Input.Key.BrowserSearch => "BROWSER_SEARCH",
            System.Windows.Input.Key.BrowserFavorites => "BROWSER_FAVORITES",
            System.Windows.Input.Key.VolumeMute => "VOLUME_MUTE",
            System.Windows.Input.Key.VolumeDown => "VOLUME_DOWN",
            System.Windows.Input.Key.VolumeUp => "VOLUME_UP",
            System.Windows.Input.Key.MediaNextTrack => "MEDIA_NEXT_TRACK",
            System.Windows.Input.Key.MediaPreviousTrack => "MEDIA_PREV_TRACK",
            System.Windows.Input.Key.MediaStop => "MEDIA_STOP",
            System.Windows.Input.Key.MediaPlayPause => "MEDIA_PLAY_PAUSE",
            System.Windows.Input.Key.LaunchMail => "LAUNCH_MAIL",
            System.Windows.Input.Key.LaunchApplication1 => "LAUNCH_APP1",
            System.Windows.Input.Key.LaunchApplication2 => "LAUNCH_APP2",
            System.Windows.Input.Key.Oem1 => "OEM_1",
            System.Windows.Input.Key.Oem2 => "OEM_2",
            System.Windows.Input.Key.Oem3 => "OEM_3",
            System.Windows.Input.Key.Oem4 => "OEM_4",
            System.Windows.Input.Key.Oem5 => "OEM_5",
            System.Windows.Input.Key.Oem6 => "OEM_6",
            System.Windows.Input.Key.Oem7 => "OEM_7",
            System.Windows.Input.Key.OemComma => "OEM_COMMA",
            System.Windows.Input.Key.OemPeriod => "OEM_PERIOD",
            System.Windows.Input.Key.OemMinus => "OEM_MINUS",
            System.Windows.Input.Key.OemPlus => "OEM_PLUS",
            _ => null
        };
    }

    private void SyncToProperty()
    {
        var combo = BuildCombo();
        _updating = true;
        KeyCombo = combo;
        _updating = false;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        RunPreview.Text = string.IsNullOrWhiteSpace(KeyCombo) ? "(未设置)" : KeyCombo;
    }
}
