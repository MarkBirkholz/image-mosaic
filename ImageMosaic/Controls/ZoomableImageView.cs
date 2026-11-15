using System.ComponentModel;
using System.Drawing.Drawing2D;
using ImageMosaic.Core;
using ImageMosaic.Infrastructure;

namespace ImageMosaic.Controls;

/// <summary>
/// Shows the mosaic (or the original image in the same place) with mouse-wheel zoom around the cursor,
/// drag to pan and double-click to switch between "fit" and 1:1. Only the visible part is drawn, from a
/// downscaled copy when zoomed out, so huge images stay smooth.
/// In frame mode a rectangle of whole tiles with the mosaic's proportions can be placed and resized, and a
/// sharper rendering of the framed part can be shown inside it.
/// </summary>
internal sealed class ZoomableImageView : Control
{
    private const float MaxZoom = 16f;
    private const int HandleRadius = 7;

    private DisplayImage? mosaic;
    private DisplayImage? original;
    private bool showOriginal;
    private bool fitMode = true;
    private float zoom = 1f;
    private PointF offset;

    private DragMode drag;
    private Point dragStart;
    private PointF dragStartOffset;
    private CellRect dragStartFrame;
    private Point frameAnchor; // fixed corner while resizing, in cells (exclusive corner coordinates)

    private CellRect? frame;
    private DisplayImage? framePreview;
    private CellRect framePreviewRect;
    private int gridColumns;
    private int gridRows;
    private int cellSize;

    public ZoomableImageView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        BackColor = Color.FromArgb(38, 38, 40);
        ForeColor = Color.FromArgb(170, 170, 175);
    }

    private enum DragMode
    {
        None,
        Pan,
        MoveFrame,
        ResizeFrame,
    }

    public event EventHandler? ZoomChanged;

    /// <summary>The frame was moved or resized.</summary>
    public event EventHandler? FrameChanged;

    /// <summary>The frame was double-clicked (confirm).</summary>
    public event EventHandler? FrameConfirmed;

    public float Zoom => zoom;

    public bool HasMosaic => mosaic != null;

    /// <summary>The selected rectangle of tiles while in frame mode; null otherwise.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CellRect? Frame => frame;

    /// <summary>Show the original image instead of the mosaic (when both exist).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowOriginal
    {
        get => showOriginal;
        set
        {
            if (showOriginal != value)
            {
                showOriginal = value;
                Invalidate();
            }
        }
    }

    /// <summary>Hint drawn when there is nothing to show.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyText { get; set; } = "";

    /// <summary>
    /// Replaces the mosaic of the same picture. The same part of the picture stays on screen at the same scale,
    /// even when the new mosaic has another size in pixels.
    /// </summary>
    public void SetMosaic(DisplayImage? image)
    {
        var previousSize = ContentSize;
        mosaic?.Dispose();
        mosaic = image;
        if (image == null)
        {
            EndFrame();
        }

        var size = ContentSize;
        if (fitMode || previousSize.IsEmpty || size.IsEmpty)
        {
            AfterContentChanged(keepView: false);
        }
        else if (size != previousSize)
        {
            // The picture keeps its size on screen: the offset stays, the zoom follows the pixel size.
            zoom = Math.Clamp(zoom * previousSize.Width / size.Width, 0.001f, MaxZoom);
            ClampOffset();
            Changed();
        }
        else
        {
            Invalidate();
        }
    }

    public void SetOriginal(DisplayImage? image)
    {
        var previousSize = ContentSize;
        original?.Dispose();
        original = image;
        AfterContentChanged(ContentSize == previousSize && previousSize != Size.Empty);
    }

    public void FitToWindow()
    {
        fitMode = true;
        var content = ContentSize;
        if (content.IsEmpty || ClientSize.Width < 2 || ClientSize.Height < 2)
        {
            return;
        }

        var margin = 8;
        zoom = Math.Min(
            (ClientSize.Width - 2f * margin) / content.Width,
            (ClientSize.Height - 2f * margin) / content.Height);
        zoom = Math.Max(zoom, 0.001f);
        offset = new PointF(
            (ClientSize.Width - content.Width * zoom) / 2f,
            (ClientSize.Height - content.Height * zoom) / 2f);
        Changed();
    }

    public void ActualSize() => ZoomAt(new Point(ClientSize.Width / 2, ClientSize.Height / 2), 1f);

    /// <summary>
    /// Starts frame mode on a mosaic of <paramref name="columns"/> × <paramref name="rows"/> tiles of
    /// <paramref name="tileSize"/> pixels. The frame starts at a third of the width, centered on the view.
    /// </summary>
    public void BeginFrame(int columns, int rows, int tileSize)
    {
        gridColumns = columns;
        gridRows = rows;
        cellSize = tileSize;
        var cols = Math.Max(1, columns / 3);
        var size = FrameSizeFor(cols);
        var center = ScreenToCell(new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f));
        frame = Place(new CellRect((int)Math.Round(center.X - size.Width / 2.0), (int)Math.Round(center.Y - size.Height / 2.0), size.Width, size.Height));
        Invalidate();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    public void EndFrame()
    {
        if (frame == null)
        {
            return;
        }

        frame = null;
        SetFramePreview(null, default);
        drag = DragMode.None;
        Invalidate();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows <paramref name="image"/> inside the frame while the frame is at <paramref name="rect"/>, instead of
    /// the mosaic's own pixels there; null removes it. The view takes ownership of the image.
    /// </summary>
    public void SetFramePreview(DisplayImage? image, CellRect rect)
    {
        framePreview?.Dispose();
        framePreview = image;
        framePreviewRect = rect;
        Invalidate();
    }

    /// <summary>Grows (positive) or shrinks (negative) the frame by whole tiles around its center.</summary>
    public void ResizeFrame(int stepsOfTenPercent)
    {
        if (frame is not { } f)
        {
            return;
        }

        var step = Math.Max(1, (int)Math.Round(f.Columns * 0.1));
        var size = FrameSizeFor(f.Columns + Math.Sign(stepsOfTenPercent) * step);
        var cx = f.Column + f.Columns / 2.0;
        var cy = f.Row + f.Rows / 2.0;
        SetFrame(new CellRect((int)Math.Round(cx - size.Width / 2.0), (int)Math.Round(cy - size.Height / 2.0), size.Width, size.Height));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            mosaic?.Dispose();
            original?.Dispose();
            framePreview?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (ContentSize.IsEmpty)
        {
            return;
        }

        if (frame != null && (ModifierKeys & Keys.Control) == 0)
        {
            ResizeFrame(e.Delta);
            return;
        }

        var factor = MathF.Pow(1.2f, e.Delta / 120f);
        ZoomAt(e.Location, zoom * factor);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button != MouseButtons.Left || ContentSize.IsEmpty)
        {
            return;
        }

        dragStart = e.Location;
        dragStartOffset = offset;
        drag = DragMode.Pan;
        if (frame is { } f)
        {
            dragStartFrame = f;
            var corner = HitCorner(e.Location, f);
            if (corner != null)
            {
                drag = DragMode.ResizeFrame;
                frameAnchor = corner.Value;
            }
            else if (FrameScreenRect(f).Contains(e.Location))
            {
                drag = DragMode.MoveFrame;
            }
        }

        Cursor = drag switch
        {
            DragMode.ResizeFrame => Cursors.SizeNWSE,
            _ => Cursors.SizeAll,
        };
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        switch (drag)
        {
            case DragMode.Pan:
                fitMode = false;
                offset = new PointF(dragStartOffset.X + e.X - dragStart.X, dragStartOffset.Y + e.Y - dragStart.Y);
                ClampOffset();
                Invalidate();
                break;
            case DragMode.MoveFrame:
                var dx = (int)Math.Round((e.X - dragStart.X) / (cellSize * zoom));
                var dy = (int)Math.Round((e.Y - dragStart.Y) / (cellSize * zoom));
                SetFrame(dragStartFrame with { Column = dragStartFrame.Column + dx, Row = dragStartFrame.Row + dy });
                break;
            case DragMode.ResizeFrame:
                ResizeToward(ScreenToCell(e.Location));
                break;
            default:
                if (frame is { } f)
                {
                    Cursor = HitCorner(e.Location, f) != null ? Cursors.SizeNWSE
                        : FrameScreenRect(f).Contains(e.Location) ? Cursors.SizeAll
                        : Cursors.Default;
                }

                break;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        drag = DragMode.None;
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (frame is { } f)
        {
            if (FrameScreenRect(f).Contains(e.Location))
            {
                FrameConfirmed?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (fitMode)
        {
            ZoomAt(e.Location, 1f);
        }
        else
        {
            FitToWindow();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (fitMode)
        {
            FitToWindow();
        }
        else
        {
            ClampOffset();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        var layer = showOriginal || mosaic == null ? original : mosaic;
        var content = ContentSize;
        if (layer == null || content.IsEmpty)
        {
            TextRenderer.DrawText(g, EmptyText, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        var dest = new RectangleF(offset.X, offset.Y, content.Width * zoom, content.Height * zoom);
        DrawLayer(g, layer, dest);

        if (frame is { } f)
        {
            // The preview belongs to one position and size of the frame; until a new one is ready, the mosaic shows.
            if (framePreview != null && f == framePreviewRect && !showOriginal)
            {
                DrawLayer(g, framePreview, FrameScreenRect(f));
            }

            PaintFrame(g, f, dest);
        }
    }

    /// <summary>Draws the part of <paramref name="layer"/> that is on screen when the whole layer covers <paramref name="dest"/>.</summary>
    private void DrawLayer(Graphics g, DisplayImage layer, RectangleF dest)
    {
        var visible = RectangleF.Intersect(dest, ClientRectangle);
        if (visible.IsEmpty)
        {
            return;
        }

        // Screen pixels per layer pixel, and the visible part in layer pixels.
        var scaleX = dest.Width / layer.Size.Width;
        var scaleY = dest.Height / layer.Size.Height;
        var layerRect = new RectangleF(
            (visible.X - dest.X) / scaleX,
            (visible.Y - dest.Y) / scaleY,
            visible.Width / scaleX,
            visible.Height / scaleY);

        // Use the downscaled copy while it still has enough pixels for the screen.
        var bitmap = layer.Full;
        if (layer.Preview != null && dest.Width <= layer.Preview.Width)
        {
            bitmap = layer.Preview;
        }

        var sx = (float)bitmap.Width / layer.Size.Width;
        var sy = (float)bitmap.Height / layer.Size.Height;
        var source = new RectangleF(layerRect.X * sx, layerRect.Y * sy, layerRect.Width * sx, layerRect.Height * sy);
        var magnification = scaleX / sx;

        g.InterpolationMode = magnification >= 2f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(bitmap, visible, source, GraphicsUnit.Pixel);
    }

    private void PaintFrame(Graphics g, CellRect f, RectangleF image)
    {
        var rect = FrameScreenRect(f);
        g.PixelOffsetMode = PixelOffsetMode.Default;
        g.SmoothingMode = SmoothingMode.None;

        // Darken everything outside the frame.
        using (var outside = new Region(image))
        {
            outside.Exclude(rect);
            using var shade = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            g.FillRegion(shade, outside);
        }

        using var pen = new Pen(Color.White, 2);
        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        using var handle = new SolidBrush(Color.White);
        foreach (var corner in Corners(rect))
        {
            g.FillRectangle(handle, corner.X - 4, corner.Y - 4, 8, 8);
        }

        var label = $"{f.Columns} × {f.Rows}";
        var labelSize = TextRenderer.MeasureText(label, Font);
        var labelRect = new Rectangle((int)rect.X + 4, (int)rect.Y + 4, labelSize.Width + 6, labelSize.Height + 2);
        using var labelBack = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        g.FillRectangle(labelBack, labelRect);
        TextRenderer.DrawText(g, label, Font, labelRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private Size ContentSize => mosaic?.Size ?? original?.Size ?? Size.Empty;

    private void AfterContentChanged(bool keepView)
    {
        if (!keepView || fitMode)
        {
            FitToWindow();
        }

        Invalidate();
    }

    private void ZoomAt(Point anchor, float newZoom)
    {
        var content = ContentSize;
        if (content.IsEmpty)
        {
            return;
        }

        var fitZoom = Math.Min((float)ClientSize.Width / content.Width, (float)ClientSize.Height / content.Height);
        newZoom = Math.Clamp(newZoom, Math.Min(fitZoom, 1f) * 0.5f, MaxZoom);
        var imageX = (anchor.X - offset.X) / zoom;
        var imageY = (anchor.Y - offset.Y) / zoom;
        zoom = newZoom;
        offset = new PointF(anchor.X - imageX * zoom, anchor.Y - imageY * zoom);
        fitMode = false;
        ClampOffset();
        Changed();
    }

    /// <summary>Keeps the image on screen: centered when smaller than the view, no empty margins when larger.</summary>
    private void ClampOffset()
    {
        var content = ContentSize;
        var w = content.Width * zoom;
        var h = content.Height * zoom;
        offset.X = w <= ClientSize.Width ? (ClientSize.Width - w) / 2f : Math.Clamp(offset.X, ClientSize.Width - w, 0);
        offset.Y = h <= ClientSize.Height ? (ClientSize.Height - h) / 2f : Math.Clamp(offset.Y, ClientSize.Height - h, 0);
    }

    private void Changed()
    {
        Invalidate();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    // ----- Frame geometry (frame coordinates are whole cells) -----

    /// <summary>A frame size with the mosaic's proportions for the given number of columns, fitting the mosaic.</summary>
    private Size FrameSizeFor(int columns)
    {
        columns = Math.Clamp(columns, 1, gridColumns);
        var rows = (int)Math.Round((double)columns * gridRows / gridColumns);
        if (rows > gridRows)
        {
            rows = gridRows;
            columns = Math.Clamp((int)Math.Round((double)rows * gridColumns / gridRows), 1, gridColumns);
        }

        return new Size(columns, Math.Max(1, rows));
    }

    private CellRect Place(CellRect f) => f with
    {
        Column = Math.Clamp(f.Column, 0, gridColumns - f.Columns),
        Row = Math.Clamp(f.Row, 0, gridRows - f.Rows),
    };

    private void SetFrame(CellRect f)
    {
        var placed = Place(f);
        if (frame == placed)
        {
            return;
        }

        frame = placed;
        Invalidate();
        FrameChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Resizes the frame from the fixed corner toward the cell under the mouse, keeping proportions.</summary>
    private void ResizeToward(PointF cell)
    {
        var dx = cell.X - frameAnchor.X;
        var dy = cell.Y - frameAnchor.Y;
        // The larger of the two movements decides, converted to columns.
        var wanted = Math.Max(Math.Abs(dx), Math.Abs(dy) * gridColumns / gridRows);
        var size = FrameSizeFor((int)Math.Round(wanted));
        var column = dx < 0 ? frameAnchor.X - size.Width : frameAnchor.X;
        var row = dy < 0 ? frameAnchor.Y - size.Height : frameAnchor.Y;
        SetFrame(new CellRect(column, row, size.Width, size.Height));
    }

    private RectangleF FrameScreenRect(CellRect f) => new(
        offset.X + f.Column * cellSize * zoom,
        offset.Y + f.Row * cellSize * zoom,
        f.Columns * cellSize * zoom,
        f.Rows * cellSize * zoom);

    private PointF ScreenToCell(PointF screen) => new(
        (screen.X - offset.X) / (cellSize * zoom),
        (screen.Y - offset.Y) / (cellSize * zoom));

    private static PointF[] Corners(RectangleF r) =>
        [new(r.Left, r.Top), new(r.Right, r.Top), new(r.Left, r.Bottom), new(r.Right, r.Bottom)];

    /// <summary>If the point is on a corner handle, the opposite corner (in cells), which stays fixed while resizing.</summary>
    private Point? HitCorner(Point point, CellRect f)
    {
        var corners = Corners(FrameScreenRect(f));
        Point[] opposite =
        [
            new(f.Column + f.Columns, f.Row + f.Rows),
            new(f.Column, f.Row + f.Rows),
            new(f.Column + f.Columns, f.Row),
            new(f.Column, f.Row),
        ];
        for (var i = 0; i < corners.Length; i++)
        {
            if (Math.Abs(point.X - corners[i].X) <= HandleRadius && Math.Abs(point.Y - corners[i].Y) <= HandleRadius)
            {
                return opposite[i];
            }
        }

        return null;
    }
}
