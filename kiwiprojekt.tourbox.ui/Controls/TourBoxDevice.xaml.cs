using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using kiwiprojekt.tourbox.ui.Models;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using Panel = System.Windows.Controls.Panel;
using Ellipse = System.Windows.Shapes.Ellipse;
using UserControl = System.Windows.Controls.UserControl;

namespace kiwiprojekt.tourbox.ui.Controls;

public partial class TourBoxDevice : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(nameof(VisualState), typeof(TourBoxVisualState),
            typeof(TourBoxDevice), new PropertyMetadata(null, OnVisualStateChanged));

    public static readonly DependencyProperty MappingsProperty =
        DependencyProperty.Register(nameof(Mappings), typeof(Dictionary<string, string>),
            typeof(TourBoxDevice), new PropertyMetadata(null, OnMappingsChanged));

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

    // Map control name -> (FrameworkElement, default background)
    private readonly Dictionary<string, FrameworkElement> _controls = new();
    private readonly SolidColorBrush _btnOff = new(Color.FromRgb(0x3A, 0x3A, 0x3A));
    private readonly SolidColorBrush _btnActive = new(Color.FromRgb(0x4C, 0xAF, 0x50));
    private readonly SolidColorBrush _knobOff = new(Color.FromRgb(0x4A, 0x4A, 0x4A));
    private readonly SolidColorBrush _knobActive = new(Color.FromRgb(0x21, 0x96, 0xF3));
    private readonly SolidColorBrush _btnHover = new(Color.FromRgb(0x55, 0x55, 0x55));

    // Label overlays
    private readonly Dictionary<string, TextBlock> _labels = new();

    private static readonly Dictionary<string, string> ControlLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tall"] = "Tall", ["Short"] = "Short", ["Top"] = "Top", ["Side"] = "Side",
        ["Up"] = "▲", ["Down"] = "▼", ["Left"] = "◀", ["Right"] = "▶",
        ["C1"] = "C1", ["C2"] = "C2", ["Tour"] = "Tour",
        ["Knob"] = "⭮", ["Scroll"] = "⟳", ["Dial"] = "◎",
    };

    public TourBoxDevice()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _controls.Clear();
        FindControls(this);
        AddLabelOverlays();
    }

    private void FindControls(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            string? tag = null;

            if (child is Border border && border.Tag is string borderTag)
                tag = borderTag;
            else if (child is Ellipse ellipse && ellipse.Tag is string ellipseTag)
                tag = ellipseTag;

            if (tag != null && child is FrameworkElement fe)
            {
                _controls[tag] = fe;
                fe.Cursor = Cursors.Hand;
                fe.MouseLeftButtonDown += (_, _) => ControlClicked?.Invoke(tag);
                fe.MouseEnter += (_, _) => OnControlHover(tag, true);
                fe.MouseLeave += (_, _) => OnControlHover(tag, false);
            }

            FindControls(child);
        }
    }

    private void AddLabelOverlays()
    {
        foreach (var (name, element) in _controls)
        {
            if (!ControlLabels.TryGetValue(name, out var label)) continue;

            var parent = VisualTreeHelper.GetParent(element) as Panel;
            if (parent == null) continue;

            var tb = new TextBlock
            {
                Text = label,
                FontSize = name switch
                {
                    "Knob" or "Scroll" or "Dial" => 26,
                    "Tall" => 22,
                    "Up" or "Down" or "Left" or "Right" => 14,
                    _ => 12
                },
                FontWeight = name is "Tall" ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            // Place label on top of the control
            if (element is Border)
            {
                // Use Adorner-like approach: add to the same parent grid cell
                var grid = FindParent<Grid>(element);
                if (grid != null)
                {
                    // Set same row/column
                    var row = Grid.GetRow(element);
                    var col = Grid.GetColumn(element);
                    var rowSpan = Grid.GetRowSpan(element);
                    var colSpan = Grid.GetColumnSpan(element);
                    Grid.SetRow(tb, row);
                    Grid.SetColumn(tb, col);
                    Grid.SetRowSpan(tb, rowSpan);
                    Grid.SetColumnSpan(tb, colSpan);
                    grid.Children.Add(tb);
                    _labels[name] = tb;
                    continue;
                }
            }

            // For non-Grid layouts, add to parent
            parent.Children.Add(tb);
            _labels[name] = tb;
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T t) return t;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private void OnControlHover(string name, bool hover)
    {
        if (_controls.TryGetValue(name, out var element))
        {
            if (hover && element is Border b && b.Background == _btnOff)
            {
                b.Background = _btnHover;
            }
            else if (!hover && element is Border b2 && b2.Background == _btnHover)
            {
                b2.Background = _btnOff;
            }
        }
    }

    private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TourBoxDevice c && e.NewValue is TourBoxVisualState s)
        {
            c.ApplyHighlights(s);
            if (e.OldValue is TourBoxVisualState old)
                old.PropertyChanged -= c.OnStateChanged;
            s.PropertyChanged += c.OnStateChanged;
        }
    }

    private void OnStateChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (s is TourBoxVisualState state) ApplyHighlights(state);
    }

    private static void OnMappingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TourBoxDevice c && e.NewValue is Dictionary<string, string> m)
            c.UpdateTooltips(m);
    }

    private readonly Dictionary<string, DispatcherTimer> _flashTimers = new();

    private void ApplyHighlights(TourBoxVisualState s)
    {
        Dispatcher.BeginInvoke(() =>
        {
            HighlightButton("Tall", s.Tall);
            HighlightButton("Short", s.Short);
            HighlightButton("Top", s.Top);
            HighlightButton("Side", s.Side);
            HighlightButton("Up", s.Up);
            HighlightButton("Down", s.Down);
            HighlightButton("Left", s.Left);
            HighlightButton("Right", s.Right);
            HighlightButton("C1", s.C1);
            HighlightButton("C2", s.C2);
            HighlightButton("Tour", s.Tour);

            FlashKnob("Knob", s.Knob || s.KnobDir != "");
            FlashKnob("Scroll", s.Scroll || s.ScrollDir != "");
            FlashKnob("Dial", s.Dial || s.DialDir != "");
        });
    }

    private void HighlightButton(string name, bool active)
    {
        if (_controls.TryGetValue(name, out var element) && element is Border border)
            border.Background = active ? _btnActive : _btnOff;
    }

    private void FlashKnob(string name, bool active)
    {
        if (!_controls.TryGetValue(name, out var element)) return;

        if (_flashTimers.TryGetValue(name, out var t))
        { t.Stop(); _flashTimers.Remove(name); }

        if (element is Ellipse ellipse)
            ellipse.Fill = active ? _knobActive : _knobOff;
        else if (element is Border border)
            border.Background = active ? _knobActive : _knobOff;

        if (active)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (_, _) =>
            {
                if (element is Ellipse e) e.Fill = _knobOff;
                else if (element is Border b) b.Background = _knobOff;
                timer.Stop();
                _flashTimers.Remove(name);
            };
            _flashTimers[name] = timer;
            timer.Start();
        }
    }

    private void UpdateTooltips(Dictionary<string, string> mappings)
    {
        foreach (var (name, element) in _controls)
        {
            var desc = mappings.TryGetValue(name, out var m) ? m : "点击编辑映射";
            element.ToolTip = $"{name} — {desc}";
        }
    }
}
