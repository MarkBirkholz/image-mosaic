using System.ComponentModel;
using System.Windows.Forms.VisualStyles;

namespace ImageMosaic.Controls;

/// <summary>
/// A horizontal slider with two handles that select a range of 0–100. Values move in <see cref="Step"/> steps;
/// the low handle never passes the high one. Mouse: drag a handle or click the track to move the nearer
/// handle there. Keyboard: arrows move the handle that was used last, Tab switches between the handles.
/// </summary>
internal sealed class RangeSlider : Control
{
    private const int Minimum = 0;
    private const int Maximum = 100;
    private int low;
    private int high = 100;
    private bool highActive;
    private bool dragging;

    public RangeSlider()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        TabStop = true;
        Height = 28;
    }

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Step { get; set; } = 5;

    public int Low => low;

    public int High => high;

    /// <summary>Sets both ends, snapped to steps and kept in order.</summary>
    public void SetRange(int newLow, int newHigh)
    {
        newLow = Snap(newLow);
        newHigh = Snap(newHigh);
        if (newLow > newHigh)
        {
            (newLow, newHigh) = (newHigh, newLow);
        }

        if (newLow == low && newHigh == high)
        {
            return;
        }

        low = newLow;
        high = newHigh;
        Invalidate();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var thumb = ThumbSize();
        var track = TrackBounds();

        if (TrackBarRenderer.IsSupported)
        {
            TrackBarRenderer.DrawHorizontalTrack(g, track);
        }
        else
        {
            ControlPaint.DrawBorder3D(g, track, Border3DStyle.Sunken);
        }

        // The selected part of the track.
        var from = PositionOf(low);
        var to = PositionOf(high);
        using (var brush = new SolidBrush(Enabled ? SystemColors.Highlight : SystemColors.GrayText))
        {
            g.FillRectangle(brush, from, track.Y, Math.Max(1, to - from), track.Height);
        }

        DrawThumb(g, ThumbBounds(low, thumb), !highActive);
        DrawThumb(g, ThumbBounds(high, thumb), highActive);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        var lowX = PositionOf(low);
        var highX = PositionOf(high);
        // On top of each other, the side the mouse is on decides which handle moves.
        highActive = lowX == highX ? e.X > highX : Math.Abs(e.X - highX) < Math.Abs(e.X - lowX);
        dragging = true;
        Capture = true;
        MoveActive(ValueAt(e.X));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging)
        {
            MoveActive(ValueAt(e.X));
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragging = false;
        Capture = false;
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown ||
        base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var current = highActive ? high : low;
        int? target = e.KeyCode switch
        {
            Keys.Left or Keys.Down => current - Step,
            Keys.Right or Keys.Up => current + Step,
            Keys.PageDown => current - Step * 4,
            Keys.PageUp => current + Step * 4,
            Keys.Home => Minimum,
            Keys.End => Maximum,
            _ => null,
        };
        if (target is { } value)
        {
            e.Handled = true;
            MoveActive(value);
        }
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        // Tab moves to the other handle first, then on to the next control.
        if ((keyData & Keys.KeyCode) == Keys.Tab && (keyData & (Keys.Control | Keys.Alt)) == 0)
        {
            var back = (keyData & Keys.Shift) != 0;
            if (back == highActive)
            {
                highActive = !highActive;
                Invalidate();
                return true;
            }
        }

        return base.ProcessDialogKey(keyData);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    private void MoveActive(int value)
    {
        value = Snap(value);
        if (highActive)
        {
            SetRange(low, Math.Max(low, value));
        }
        else
        {
            SetRange(Math.Min(value, high), high);
        }

        Invalidate();
    }

    private int Snap(int value)
    {
        var step = Math.Max(1, Step);
        var snapped = Minimum + (int)Math.Round((value - Minimum) / (double)step) * step;
        return Math.Clamp(snapped, Minimum, Maximum);
    }

    private Size ThumbSize()
    {
        var scale = DeviceDpi / 96f;
        return new Size((int)Math.Round(11 * scale), (int)Math.Round(20 * scale));
    }

    private Rectangle TrackBounds()
    {
        var half = ThumbSize().Width / 2 + 1;
        var height = Math.Max(4, (int)Math.Round(4 * DeviceDpi / 96f));
        return new Rectangle(half, (Height - height) / 2, Math.Max(1, Width - 2 * half), height);
    }

    private int PositionOf(int value)
    {
        var track = TrackBounds();
        var share = Maximum == Minimum ? 0 : (value - Minimum) / (double)(Maximum - Minimum);
        return track.X + (int)Math.Round(share * track.Width);
    }

    private int ValueAt(int x)
    {
        var track = TrackBounds();
        var share = Math.Clamp((x - track.X) / (double)track.Width, 0, 1);
        return Minimum + (int)Math.Round(share * (Maximum - Minimum));
    }

    private Rectangle ThumbBounds(int value, Size thumb) =>
        new(PositionOf(value) - thumb.Width / 2, (Height - thumb.Height) / 2, thumb.Width, thumb.Height);

    private void DrawThumb(Graphics g, Rectangle bounds, bool active)
    {
        if (TrackBarRenderer.IsSupported)
        {
            var state = !Enabled ? TrackBarThumbState.Disabled
                : active && (Focused || dragging) ? TrackBarThumbState.Pressed
                : TrackBarThumbState.Normal;
            TrackBarRenderer.DrawHorizontalThumb(g, bounds, state);
        }
        else
        {
            ControlPaint.DrawButton(g, bounds, Enabled ? ButtonState.Normal : ButtonState.Inactive);
        }

        if (active && Focused && ShowFocusCues)
        {
            var focus = bounds;
            focus.Inflate(2, 2);
            ControlPaint.DrawFocusRectangle(g, focus);
        }
    }
}
