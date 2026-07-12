namespace kiwiprojekt.tourbox.ui.Models;

/// <summary>
/// Tracks the visual highlight state of each TourBox control.
/// </summary>
public class TourBoxVisualState : BindableBase
{
    private bool _tall;
    public bool Tall { get => _tall; set => SetProperty(ref _tall, value); }

    private bool _side;
    public bool Side { get => _side; set => SetProperty(ref _side, value); }

    private bool _top;
    public bool Top { get => _top; set => SetProperty(ref _top, value); }

    private bool _short;
    public bool Short { get => _short; set => SetProperty(ref _short, value); }

    private bool _knob;
    public bool Knob { get => _knob; set => SetProperty(ref _knob, value); }

    private bool _scroll;
    public bool Scroll { get => _scroll; set => SetProperty(ref _scroll, value); }

    private bool _dial;
    public bool Dial { get => _dial; set => SetProperty(ref _dial, value); }

    private bool _up;
    public bool Up { get => _up; set => SetProperty(ref _up, value); }

    private bool _down;
    public bool Down { get => _down; set => SetProperty(ref _down, value); }

    private bool _left;
    public bool Left { get => _left; set => SetProperty(ref _left, value); }

    private bool _right;
    public bool Right { get => _right; set => SetProperty(ref _right, value); }

    private bool _c1;
    public bool C1 { get => _c1; set => SetProperty(ref _c1, value); }

    private bool _c2;
    public bool C2 { get => _c2; set => SetProperty(ref _c2, value); }

    private bool _tour;
    public bool Tour { get => _tour; set => SetProperty(ref _tour, value); }

    private string _knobDir = "";
    public string KnobDir { get => _knobDir; set => SetProperty(ref _knobDir, value); }

    private string _scrollDir = "";
    public string ScrollDir { get => _scrollDir; set => SetProperty(ref _scrollDir, value); }

    private string _dialDir = "";
    public string DialDir { get => _dialDir; set => SetProperty(ref _dialDir, value); }

    /// <summary>
    /// Reset all highlights.
    /// </summary>
    public void ClearAll()
    {
        Tall = Side = Top = Short = false;
        Knob = Scroll = Dial = false;
        Up = Down = Left = Right = false;
        C1 = C2 = Tour = false;
        KnobDir = ScrollDir = DialDir = "";
    }

    /// <summary>
    /// Apply a TourBoxEvent to update visual state.
    /// </summary>
    public void ApplyEvent(TourBoxEvent e)
    {
        var primaryKey = e.Keys.Length > 0 ? e.Keys[0] : (TourBoxKey?)null;
        if (primaryKey == null) return;

        switch (e.Action)
        {
            case ActionType.Click:
                SetPressed(primaryKey.Value, true);
                break;
            case ActionType.ClickReleased:
                SetPressed(primaryKey.Value, false);
                break;
            case ActionType.Increased:
                SetRotaryDir(primaryKey.Value, "▲");
                break;
            case ActionType.Decreased:
                SetRotaryDir(primaryKey.Value, "▼");
                break;
        }
    }

    private void SetPressed(TourBoxKey key, bool pressed)
    {
        switch (key)
        {
            case TourBoxKey.Tall: Tall = pressed; break;
            case TourBoxKey.Side: Side = pressed; break;
            case TourBoxKey.Top: Top = pressed; break;
            case TourBoxKey.Short: Short = pressed; break;
            case TourBoxKey.Knob: Knob = pressed; break;
            case TourBoxKey.Scroll: Scroll = pressed; break;
            case TourBoxKey.Dial: Dial = pressed; break;
            case TourBoxKey.Up: Up = pressed; break;
            case TourBoxKey.Down: Down = pressed; break;
            case TourBoxKey.Left: Left = pressed; break;
            case TourBoxKey.Right: Right = pressed; break;
            case TourBoxKey.C1: C1 = pressed; break;
            case TourBoxKey.C2: C2 = pressed; break;
            case TourBoxKey.Tour: Tour = pressed; break;
        }
    }

    private void SetRotaryDir(TourBoxKey key, string dir)
    {
        switch (key)
        {
            case TourBoxKey.Knob: KnobDir = dir; break;
            case TourBoxKey.Scroll: ScrollDir = dir; break;
            case TourBoxKey.Dial: DialDir = dir; break;
        }
    }
}
