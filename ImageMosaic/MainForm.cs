using System.Globalization;
using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;
using ImageMosaic.Infrastructure;

namespace ImageMosaic;

internal sealed partial class MainForm : Form
{
    private const int TargetPreviewShortSide = 1600;

    private readonly MosaicBuilder builder;
    private readonly System.Windows.Forms.Timer rerenderTimer = new() { Interval = 120 };
    private CancellationTokenSource? buildCts;
    private CancellationTokenSource? rerenderCts;
    private MosaicResult? result;
    private ImageInfo? targetInfo;
    private string? shownTargetPath;
    private int targetVersion;
    private int scanVersion;
    private bool building;

    public MainForm()
    {
        BuildLayout();
        builder = new MosaicBuilder(TileCache.DefaultDirectory, Log);
        ApplySettings(SettingsStore.Load());
        WireEvents();
        UpdateSliderLabels();
        UpdateOutputSize();
        UpdateState();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        split.SplitterDistance = Math.Max(100, split.Height - LogicalToDeviceUnits(130));
        ShowTarget(targetBox.Text);
        CountTiles(tilesBox.Text);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        buildCts?.Cancel();
        rerenderCts?.Cancel();
        SettingsStore.Save(CurrentSettings());
        base.OnFormClosing(e);
    }

    private void WireEvents()
    {
        targetBrowse.Click += (_, _) => BrowseTarget();
        tilesBrowse.Click += (_, _) => BrowseTiles();
        targetBox.Leave += (_, _) => ShowTarget(targetBox.Text);
        tilesBox.Leave += (_, _) => CountTiles(tilesBox.Text);
        targetBox.KeyDown += (_, e) => OnEnter(e, () => ShowTarget(targetBox.Text));
        tilesBox.KeyDown += (_, e) => OnEnter(e, () => CountTiles(tilesBox.Text));

        columnsInput.ValueChanged += (_, _) => UpdateOutputSize();
        tileSizeInput.ValueChanged += (_, _) => UpdateOutputSize();
        varietyBar.ValueChanged += (_, _) => UpdateSliderLabels();
        correctionBar.ValueChanged += (_, _) => ColorsChanged();
        overlayBar.ValueChanged += (_, _) => ColorsChanged();
        rerenderTimer.Tick += async (_, _) => await RerenderAsync();

        buildButton.Click += async (_, _) => await BuildOrCancelAsync();
        saveButton.Click += async (_, _) => await SaveAsync();

        originalToggle.CheckedChanged += (_, _) => viewer.ShowOriginal = originalToggle.Checked;
        fitButton.Click += (_, _) => viewer.FitToWindow();
        actualSizeButton.Click += (_, _) => viewer.ActualSize();
        viewer.ZoomChanged += (_, _) => zoomLabel.Text = viewer.Zoom.ToString("P0", CultureInfo.CurrentCulture);

        KeyDown += OnFormKeyDown;
        KeyUp += OnFormKeyUp;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
    }

    // ----- Settings -----

    private void ApplySettings(MosaicSettings s)
    {
        targetBox.Text = s.TargetImagePath;
        tilesBox.Text = s.TilesFolder;
        columnsInput.Value = Math.Clamp(s.Columns, (int)columnsInput.Minimum, (int)columnsInput.Maximum);
        tileSizeInput.Value = Math.Clamp(s.TileSize, (int)tileSizeInput.Minimum, (int)tileSizeInput.Maximum);
        gridInput.Value = Math.Clamp(s.FeatureGrid, (int)gridInput.Minimum, (int)gridInput.Maximum);
        repeatInput.Value = Math.Clamp(s.MinRepeatDistance, (int)repeatInput.Minimum, (int)repeatInput.Maximum);
        varietyBar.Value = Math.Clamp((int)Math.Round(s.UsagePenalty), varietyBar.Minimum, varietyBar.Maximum);
        correctionBar.Value = Math.Clamp((int)Math.Round(s.ColorCorrection * 100), 0, 100);
        overlayBar.Value = Math.Clamp((int)Math.Round(s.Overlay * 100), 0, 100);
    }

    private MosaicSettings CurrentSettings() => new()
    {
        TargetImagePath = targetBox.Text.Trim().Trim('"'),
        TilesFolder = tilesBox.Text.Trim().Trim('"'),
        Columns = (int)columnsInput.Value,
        TileSize = (int)tileSizeInput.Value,
        FeatureGrid = (int)gridInput.Value,
        MinRepeatDistance = (int)repeatInput.Value,
        UsagePenalty = varietyBar.Value,
        ColorCorrection = correctionBar.Value / 100.0,
        Overlay = overlayBar.Value / 100.0,
    };

    private void UpdateSliderLabels()
    {
        varietyValue.Text = varietyBar.Value.ToString(CultureInfo.CurrentCulture);
        correctionValue.Text = $"{correctionBar.Value}%";
        overlayValue.Text = $"{overlayBar.Value}%";
    }

    private void UpdateOutputSize()
    {
        if (targetInfo is not { } info)
        {
            outputSizeLabel.Text = "Output size depends on the target image.";
            outputSizeLabel.ForeColor = SystemColors.GrayText;
            return;
        }

        var settings = CurrentSettings();
        var (width, height, rows) = settings.OutputSize(info.Width, info.Height);
        var megapixels = (double)width * height / 1_000_000;
        var tooBig = (long)width * height > MosaicSettings.MaxOutputPixels;
        outputSizeLabel.Text = $"{settings.Columns} × {rows} = {settings.Columns * rows:N0} tiles\n" +
                               $"Output {width:N0} × {height:N0} px ({megapixels:N1} MP)" +
                               (tooBig ? " — too big" : "");
        outputSizeLabel.ForeColor = tooBig ? Color.Firebrick : SystemColors.GrayText;
    }

    // ----- Choosing inputs -----

    private void BrowseTarget()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose the picture to reproduce",
            Filter = "Images|" + string.Join(";", ImageCodec.SupportedExtensions.Select(e => "*" + e)) + "|All files|*.*",
        };
        var current = targetBox.Text.Trim();
        if (File.Exists(current))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(current);
            dialog.FileName = Path.GetFileName(current);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ShowTarget(dialog.FileName);
        }
    }

    private void BrowseTiles()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Folder with the photos to use as tiles (subfolders are included)",
            UseDescriptionForTitle = true,
        };
        if (Directory.Exists(tilesBox.Text.Trim()))
        {
            dialog.InitialDirectory = tilesBox.Text.Trim();
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            CountTiles(dialog.SelectedPath);
        }
    }

    /// <summary>Loads and shows the target picture in the background.</summary>
    private async void ShowTarget(string path)
    {
        path = NormalizePath(path);
        targetBox.Text = path;
        if (string.Equals(path, shownTargetPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var version = ++targetVersion;
        shownTargetPath = path;
        targetInfo = null;
        if (result != null && !string.Equals(result.Settings.TargetImagePath, path, StringComparison.OrdinalIgnoreCase))
        {
            result = null;
            viewer.SetMosaic(null);
        }

        if (!File.Exists(path))
        {
            viewer.SetOriginal(null);
            UpdateOutputSize();
            UpdateState();
            return;
        }

        try
        {
            var (info, display) = await Task.Run(() =>
            {
                var info = ImageCodec.ReadInfo(path);
                return (info, DisplayImage.From(ImageCodec.Load(path, TargetPreviewShortSide)));
            });
            if (version != targetVersion || IsDisposed)
            {
                display.Dispose();
                return;
            }

            targetInfo = info;
            viewer.SetOriginal(display);
        }
        catch (Exception e) when (e is IOException or NotSupportedException or UnauthorizedAccessException
                                      or System.Runtime.InteropServices.COMException or ArgumentException)
        {
            if (version == targetVersion)
            {
                viewer.SetOriginal(null);
                SetStatus($"Cannot read {Path.GetFileName(path)}: {e.Message}");
            }
        }

        UpdateOutputSize();
        UpdateState();
    }

    /// <summary>Counts the images in the tiles folder in the background, as a quick sanity check.</summary>
    private async void CountTiles(string folder)
    {
        folder = NormalizePath(folder);
        tilesBox.Text = folder;
        var version = ++scanVersion;
        if (folder.Length == 0)
        {
            tilesInfoLabel.Text = "";
            return;
        }

        if (!Directory.Exists(folder))
        {
            tilesInfoLabel.Text = "Folder not found.";
            return;
        }

        tilesInfoLabel.Text = "Counting images…";
        try
        {
            var count = await Task.Run(() => ImageFileScanner.Scan(folder).Count);
            if (version == scanVersion)
            {
                tilesInfoLabel.Text = count == 0
                    ? "No images found in this folder."
                    : $"{count:N0} images found" + (count < 100 ? " — a few hundred or more give better results." : ".");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (version == scanVersion)
            {
                tilesInfoLabel.Text = e.Message;
            }
        }
    }

    // ----- Building -----

    private async Task BuildOrCancelAsync()
    {
        if (building)
        {
            buildCts?.Cancel();
            SetStatus("Cancelling…");
            return;
        }

        var settings = CurrentSettings();
        var errors = settings.Validate();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SettingsStore.Save(settings);
        rerenderTimer.Stop();
        rerenderCts?.Cancel();
        using var cts = new CancellationTokenSource();
        buildCts = cts;
        building = true;
        originalToggle.Checked = false;
        UpdateState();
        Log($"Building {Path.GetFileName(settings.TargetImagePath)} from {settings.TilesFolder}");

        var progress = new Progress<MosaicProgress>(ShowProgress);
        try
        {
            var (built, display) = await Task.Run(() =>
            {
                var r = builder.Build(settings, progress, cts.Token);
                return (r, DisplayImage.From(r.Image));
            }, cts.Token);

            result = built;
            viewer.SetMosaic(display);
            tilesInfoLabel.Text = $"{built.Library.Tiles.Count:N0} photos usable ({built.Library.Stats.Duplicates:N0} duplicates, " +
                                  $"{built.Library.Stats.Failed:N0} unreadable).";
            SetStatus(built.Summary);
            if (CurrentSettings().ToRenderOptions() != settings.ToRenderOptions())
            {
                // Color sliders were moved while building.
                rerenderTimer.Start();
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Cancelled.");
            Log("Cancelled.");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            SetStatus(e.Message);
            Log(e.Message);
            MessageBox.Show(this, e.Message, "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception e)
        {
            SetStatus("Failed: " + e.Message);
            Log(e.ToString());
            MessageBox.Show(this, e.Message, "Image Mosaic — error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            building = false;
            buildCts = null;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            UpdateState();
        }
    }

    private void ColorsChanged()
    {
        UpdateSliderLabels();
        if (result != null && !building)
        {
            rerenderTimer.Stop();
            rerenderTimer.Start();
        }
    }

    /// <summary>Redraws the finished mosaic with the current color sliders, without matching again.</summary>
    private async Task RerenderAsync()
    {
        rerenderTimer.Stop();
        if (result is not { } current || building)
        {
            return;
        }

        rerenderCts?.Cancel();
        var cts = new CancellationTokenSource();
        rerenderCts = cts;
        var correction = correctionBar.Value / 100.0;
        var overlay = overlayBar.Value / 100.0;
        try
        {
            var (updated, display) = await Task.Run(() =>
            {
                var r = MosaicBuilder.Rerender(current, correction, overlay, null, cts.Token);
                return (r, DisplayImage.From(r.Image));
            }, cts.Token);

            if (rerenderCts == cts && !building && result == current)
            {
                result = updated;
                viewer.SetMosaic(display);
            }
            else
            {
                display.Dispose();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer change superseded this one.
        }
        finally
        {
            if (rerenderCts == cts)
            {
                rerenderCts = null;
            }

            cts.Dispose();
        }
    }

    private async Task SaveAsync()
    {
        if (result is not { } current || building)
        {
            return;
        }

        var target = current.Settings.TargetImagePath;
        using var dialog = new SaveFileDialog
        {
            Title = "Save mosaic",
            Filter = "JPEG (smaller file)|*.jpg|PNG (lossless)|*.png|TIFF|*.tif",
            FileName = Path.GetFileNameWithoutExtension(target) + "_mosaic.jpg",
            InitialDirectory = Path.GetDirectoryName(target),
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        saveButton.Enabled = false;
        SetStatus("Saving…");
        try
        {
            await Task.Run(() => ImageCodec.Save(current.Image, path));
            SetStatus($"Saved {Path.GetFileName(path)}");
            Log($"Saved {path}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            SetStatus("Not saved: " + e.Message);
            MessageBox.Show(this, e.Message, "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateState();
        }
    }

    // ----- UI state -----

    private void ShowProgress(MosaicProgress p)
    {
        if (!building)
        {
            return;
        }

        if (p.Stage is MosaicStage.Scanning or MosaicStage.LoadingTarget)
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }
        else
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = (int)(p.Fraction * progressBar.Maximum);
        }

        statusLabel.Text = p.ToString();
    }

    private void UpdateState()
    {
        buildButton.Text = building ? "Cancel" : "Build mosaic";
        saveButton.Enabled = !building && result != null;
        originalToggle.Enabled = result != null && viewer.HasMosaic;
        sourceGroup.Enabled = !building;
        layoutGroup.Enabled = !building;
        matchingGroup.Enabled = !building;
    }

    private void SetStatus(string text) => statusLabel.Text = text;

    /// <summary>Removes quotes and surrounding spaces (as pasted from Explorer) and expands existing paths to full form.</summary>
    private static string NormalizePath(string path)
    {
        path = path.Trim().Trim('"').Trim();
        try
        {
            return path.Length > 0 && (File.Exists(path) || Directory.Exists(path)) ? Path.GetFullPath(path) : path;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private void Log(string message)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => Log(message));
            return;
        }

        logBox.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
    }

    // ----- Keyboard and drag & drop -----

    private static void OnEnter(KeyEventArgs e, Action action)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            action();
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F5 when !building:
                e.Handled = true;
                _ = BuildOrCancelAsync();
                break;
            case Keys.Escape when building:
                e.Handled = true;
                buildCts?.Cancel();
                break;
            case Keys.S when e.Control:
                e.SuppressKeyPress = true;
                _ = SaveAsync();
                break;
            case Keys.Space when IsSpaceForViewer():
                e.SuppressKeyPress = true;
                viewer.ShowOriginal = true;
                break;
        }
    }

    private void OnFormKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space && IsSpaceForViewer())
        {
            e.SuppressKeyPress = true;
            viewer.ShowOriginal = originalToggle.Checked;
        }
    }

    /// <summary>Space compares with the original unless the user is typing.</summary>
    private bool IsSpaceForViewer() =>
        viewer.HasMosaic && ActiveControl is not TextBoxBase && ActiveControl is not NumericUpDown &&
        ActiveControl?.Parent is not NumericUpDown;

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = !building && e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (building || e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                CountTiles(path);
            }
            else if (File.Exists(path) && ImageCodec.IsSupported(path))
            {
                ShowTarget(path);
            }
        }
    }
}
