using System.ComponentModel;
using System.Drawing.Drawing2D;
using ImageMosaic.Infrastructure;

namespace ImageMosaic.Controls;

/// <summary>
/// Shows the mosaic (or the original image in the same place) with mouse-wheel zoom around the
/// cursor, drag to pan and double-click to switch between "fit" and 1:1.
/// Only the visible part is drawn, from a downscaled copy when zoomed out, so huge images stay smooth.
/// </summary>
internal sealed class ZoomableImageView : Control
{
    private const float MaxZoom = 16f;

    private DisplayImage? mosaic;
    private DisplayImage? original;
    private bool showOriginal;
    private bool fitMode = true;
    private float zoom = 1f;
    private PointF offset;
    private Point? dragStart;
    private PointF dragStartOffset;

    public ZoomableImageView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        BackColor = Color.FromArgb(38, 38, 40);
        ForeColor = Color.FromArgb(170, 170, 175);
    }

    public event EventHandler? ZoomChanged;

    public float Zoom => zoom;

    public bool HasMosaic => mosaic != null;

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

    /// <summary>Replaces the mosaic. The view keeps its zoom and position when the size is unchanged.</summary>
    public void SetMosaic(DisplayImage? image)
    {
        var keepView = image != null && mosaic != null && image.Size == mosaic.Size;
        var previousSize = ContentSize;
        mosaic?.Dispose();
        mosaic = image;
        AfterContentChanged(keepView || ContentSize == previousSize);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            mosaic?.Dispose();
            original?.Dispose();
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

        var factor = MathF.Pow(1.2f, e.Delta / 120f);
        ZoomAt(e.Location, zoom * factor);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Left && !ContentSize.IsEmpty)
        {
            dragStart = e.Location;
            dragStartOffset = offset;
            Cursor = Cursors.SizeAll;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragStart is { } start)
        {
            fitMode = false;
            offset = new PointF(dragStartOffset.X + e.X - start.X, dragStartOffset.Y + e.Y - start.Y);
            ClampOffset();
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        dragStart = null;
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
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

        // Visible part of the content, in content pixels.
        var dest = new RectangleF(offset.X, offset.Y, content.Width * zoom, content.Height * zoom);
        var visible = RectangleF.Intersect(dest, ClientRectangle);
        if (visible.IsEmpty)
        {
            return;
        }

        var contentRect = new RectangleF(
            (visible.X - offset.X) / zoom,
            (visible.Y - offset.Y) / zoom,
            visible.Width / zoom,
            visible.Height / zoom);

        // Use the downscaled copy while it still has enough pixels for the screen.
        var bitmap = layer.Full;
        if (layer.Preview != null && content.Width * zoom <= layer.Preview.Width)
        {
            bitmap = layer.Preview;
        }

        var sx = (float)bitmap.Width / content.Width;
        var sy = (float)bitmap.Height / content.Height;
        var source = new RectangleF(contentRect.X * sx, contentRect.Y * sy, contentRect.Width * sx, contentRect.Height * sy);
        var magnification = zoom / sx;

        g.InterpolationMode = magnification >= 2f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(bitmap, visible, source, GraphicsUnit.Pixel);
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
}
