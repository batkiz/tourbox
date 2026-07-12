using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using kiwiprojekt.tourbox.ui.Models;
using UserControl = System.Windows.Controls.UserControl;
using Cursors = System.Windows.Input.Cursors;

namespace kiwiprojekt.tourbox.ui.Controls;

/// <summary>
/// Visual representation of the TourBox device with real-time key highlighting.
/// </summary>
public partial class TourBoxDevice : UserControl
{
    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(
            nameof(VisualState),
            typeof(TourBoxVisualState),
            typeof(TourBoxDevice),
            new PropertyMetadata(null, OnVisualStateChanged));

    public static readonly DependencyProperty MappingsProperty =
        DependencyProperty.Register(
            nameof(Mappings),
            typeof(Dictionary<string, string>),
            typeof(TourBoxDevice),
            new PropertyMetadata(null, OnMappingsChanged));

    public event Action<string>? ControlClicked;

    public TourBoxVisualState? VisualState
    {
        get => (TourBoxVisualState?)GetValue(VisualStateProperty);
        set => SetValue(VisualStateProperty, value);
    }

    public Dictionary<string, string>? Mappings
    {
        get => (Dictionary<string, string>?)GetValue(MappingsProperty);
        set => SetValue(MappingsProperty, value);
    }

    private readonly Dictionary<string, Border> _buttonMap = new();
    private readonly Dictionary<string, DispatcherTimer> _flashTimers = new();
    private readonly SolidColorBrush _normalBrush = new(System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0));
    private readonly SolidColorBrush _activeBrush = new(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
    private readonly SolidColorBrush _rotaryBrush = new(System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3));

    public TourBoxDevice()
    {
        InitializeComponent();
    }

    private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TourBoxDevice control && e.NewValue is TourBoxVisualState state)
        {
            control.ApplyVisualState(state);
            if (e.OldValue is TourBoxVisualState oldState)
                oldState.PropertyChanged -= control.OnStatePropertyChanged;
            state.PropertyChanged += control.OnStatePropertyChanged;
        }
    }

    private static void OnMappingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TourBoxDevice control && e.NewValue is Dictionary<string, string> mappings)
        {
            control.UpdateTooltips(mappings);
        }
    }

    private void UpdateTooltips(Dictionary<string, string> mappings)
    {
        foreach (var (name, border) in _buttonMap)
        {
            var desc = mappings.TryGetValue(name, out var m) ? m : "点击编辑映射";
            border.ToolTip = $"{name} — {desc}";
        }
    }

    private void OnStatePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is TourBoxVisualState state)
            ApplyVisualState(state);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _buttonMap.Clear();
        // After template is applied, find all tagged borders and make them clickable
        FindButtons(this);
    }

    private void FindButtons(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Border border && border.Tag is string tag && !string.IsNullOrEmpty(tag))
            {
                _buttonMap[tag] = border;
                border.Cursor = Cursors.Hand;
                border.MouseLeftButtonDown += (_, _) => ControlClicked?.Invoke(tag);
                border.ToolTip = $"{tag} — 点击编辑映射";
            }
            FindButtons(child);
        }
    }

    private void ApplyVisualState(TourBoxVisualState state)
    {
        Dispatcher.BeginInvoke(() =>
        {
            UpdateButton("Tall", state.Tall);
            UpdateButton("Side", state.Side);
            UpdateButton("Top", state.Top);
            UpdateButton("Short", state.Short);
            UpdateButton("Up", state.Up);
            UpdateButton("Down", state.Down);
            UpdateButton("Left", state.Left);
            UpdateButton("Right", state.Right);
            UpdateButton("C1", state.C1);
            UpdateButton("C2", state.C2);
            UpdateButton("Tour", state.Tour);

            // Rotaries: flash on direction change
            FlashRotary("Knob", state.Knob || !string.IsNullOrEmpty(state.KnobDir));
            FlashRotary("Scroll", state.Scroll || !string.IsNullOrEmpty(state.ScrollDir));
            FlashRotary("Dial", state.Dial || !string.IsNullOrEmpty(state.DialDir));
        });
    }

    private void UpdateButton(string name, bool active)
    {
        if (_buttonMap.TryGetValue(name, out var border))
        {
            border.Background = active ? _activeBrush : _normalBrush;
        }
    }

    private void FlashRotary(string name, bool active)
    {
        if (!_buttonMap.TryGetValue(name, out var border))
            return;

        if (_flashTimers.TryGetValue(name, out var existingTimer))
        {
            existingTimer.Stop();
            existingTimer.Tick -= (_, _) => { };
        }

        border.Background = active ? _rotaryBrush : _normalBrush;

        if (active && !string.IsNullOrEmpty(VisualState?.GetType()
                .GetProperty(name + "Dir")?.GetValue(VisualState)?.ToString()))
        {
            // Flash back to normal after 300ms for rotary turns
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (_, _) =>
            {
                border.Background = _normalBrush;
                timer.Stop();
                _flashTimers.Remove(name);
            };
            _flashTimers[name] = timer;
            timer.Start();
        }
    }
}
