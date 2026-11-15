using System.Diagnostics;
using System.Globalization;
using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Localization;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Suggestions;
using ImageMosaic.Core.Tiles;
using ImageMosaic.Infrastructure;

namespace ImageMosaic;

internal sealed partial class MainForm : Form
{
    private const int TargetPreviewShortSide = 1600;
    private const int MaxFragmentTileSize = 1024;
    private const int MaxFragmentTileScale = 4;

    private readonly MosaicBuilder builder;
    private readonly System.Windows.Forms.Timer rerenderTimer = new() { Interval = 120 };
    private CancellationTokenSource? buildCts;
    private CancellationTokenSource? rerenderCts;
    private MosaicResult? result;
    private ImageInfo? targetInfo;
    private string? shownTargetPath;
    private string? lastSavedPath;
    private int targetVersion;
    private int scanVersion;
    private bool building;
    private bool savingFragment;
    private SuggestionWeights suggestionWeights = SuggestionWeights.Default;
    private SuggestionAnalysis? suggestionAnalysis;

    // Functions, so that the texts can be rendered again after switching the language.
    private Func<string> statusText = () => "";
    private Func<string> tilesInfoText = () => "";

    public MainForm()
    {
        var settings = SettingsStore.Load();
        Lang.Use(settings.Language);
        BuildLayout();
        builder = new MosaicBuilder(TileCache.DefaultDirectory);
        ApplySettings(settings);
        FillLanguages();
        WireEvents();
        ApplyLanguage();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Small screens.
        var area = Screen.FromControl(this).WorkingArea;
        if (Height > area.Height || Width > area.Width)
        {
            Bounds = new Rectangle(area.X, area.Y, Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        }

        settingsScroller.AutoScrollPosition = Point.Empty;
        ShowTarget(targetBox.Text);
        CountTiles();
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
        targetBox.Leave += (_, _) => ShowTarget(targetBox.Text);
        targetBox.KeyDown += (_, e) => OnEnter(e, () => ShowTarget(targetBox.Text));
        suggestLink.LinkClicked += (_, _) => SuggestTarget();
        clearCacheLink.LinkClicked += (_, _) => ClearCache();
        addFolderButton.Click += (_, _) => BrowseFolders();
        removeFolderButton.Click += (_, _) => RemoveSelectedFolders();
        foldersList.SelectedIndexChanged += (_, _) => UpdateState();
        foldersList.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                RemoveSelectedFolders();
            }
        };

        columnsInput.ValueChanged += (_, _) => UpdateOutputSize();
        tileSizeInput.ValueChanged += (_, _) => UpdateOutputSize();
        varietyBar.ValueChanged += (_, _) => UpdateSliderLabels();
        correctionBar.ValueChanged += (_, _) => LookChanged();
        overlayBar.ValueChanged += (_, _) => LookChanged();
        edgesBar.ValueChanged += (_, _) => LookChanged();
        rerenderTimer.Tick += async (_, _) => await RerenderAsync();

        buildButton.Click += async (_, _) => await BuildOrCancelAsync();
        saveButton.Click += async (_, _) => await SaveAsync();
        saveFragmentButton.Click += async (_, _) => await FragmentButtonAsync();
        frameSaveButton.Click += async (_, _) => await SaveFragmentAsync();
        frameCancelButton.Click += (_, _) => EndFragment();
        openSavedLink.LinkClicked += (_, _) => OpenSaved(select: false);
        showSavedLink.LinkClicked += (_, _) => OpenSaved(select: true);

        originalToggle.CheckedChanged += (_, _) => viewer.ShowOriginal = originalToggle.Checked;
        fitButton.Click += (_, _) => viewer.FitToWindow();
        actualSizeButton.Click += (_, _) => viewer.ActualSize();
        viewer.ZoomChanged += (_, _) => zoomLabel.Text = viewer.Zoom.ToString("P0", CultureInfo.CurrentCulture);
        viewer.FrameChanged += (_, _) => ShowFragmentInfo();
        viewer.FrameConfirmed += async (_, _) => await SaveFragmentAsync();
        languageBox.SelectedIndexChanged += (_, _) =>
        {
            if (languageBox.SelectedItem is Language language && language.Code != Lang.Current)
            {
                Lang.Use(language.Code);
                ApplyLanguage();
                SettingsStore.Save(CurrentSettings());
            }
        };

        KeyDown += OnFormKeyDown;
        KeyUp += OnFormKeyUp;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
    }

    // Language

    private void FillLanguages()
    {
        languageBox.Items.Clear();
        foreach (var language in Lang.Languages)
        {
            languageBox.Items.Add(language);
        }

        languageBox.SelectedItem = Lang.Languages.FirstOrDefault(l => l.Code == Lang.Current);
        languageBox.Visible = languageBox.Items.Count > 1;
    }

    private void ApplyLanguage()
    {
        SuspendLayout();
        localizer.ApplyAll();
        statusLabel.Text = statusText();
        tilesInfoLabel.Text = tilesInfoText();
        hintLabel.Text = viewer.Frame != null ? T(Texts.Main.FragmentHint) : T(Texts.Main.ViewHint);
        toolTip.SetToolTip(hintLabel, Localizer.Wrap(hintLabel.Text));
        UpdateSliderLabels();
        UpdateOutputSize();
        UpdateState();
        ResumeLayout(true);
    }

    // Settings

    private void ApplySettings(MosaicSettings s)
    {
        foldersList.Items.Clear();
        foreach (var folder in s.TilesFolders.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            foldersList.Items.Add(folder);
        }

        columnsInput.Value = Math.Clamp(s.Columns, (int)columnsInput.Minimum, (int)columnsInput.Maximum);
        tileSizeInput.Value = Math.Clamp(s.TileSize, (int)tileSizeInput.Minimum, (int)tileSizeInput.Maximum);
        gridInput.Value = Math.Clamp(s.FeatureGrid, (int)gridInput.Minimum, (int)gridInput.Maximum);
        repeatInput.Value = Math.Clamp(s.MinRepeatDistance, (int)repeatInput.Minimum, (int)repeatInput.Maximum);
        varietyBar.Value = Math.Clamp((int)Math.Round(s.UsagePenalty), varietyBar.Minimum, varietyBar.Maximum);
        correctionBar.Value = PercentSteps.ToSteps(s.ColorCorrection);
        overlayBar.Value = PercentSteps.ToSteps(s.Overlay);
        edgesBar.Value = PercentSteps.ToSteps(s.EdgeBlend);
        suggestionWeights = s.SuggestionWeights ?? SuggestionWeights.Default;
    }

    private MosaicSettings CurrentSettings() => new()
    {
        TargetImagePath = targetBox.Text.Trim().Trim('"'),
        TilesFolders = TileFolders(),
        Columns = (int)columnsInput.Value,
        TileSize = (int)tileSizeInput.Value,
        FeatureGrid = (int)gridInput.Value,
        MinRepeatDistance = (int)repeatInput.Value,
        UsagePenalty = varietyBar.Value,
        ColorCorrection = PercentSteps.ToFraction(correctionBar.Value),
        Overlay = PercentSteps.ToFraction(overlayBar.Value),
        EdgeBlend = PercentSteps.ToFraction(edgesBar.Value),
        SuggestionWeights = suggestionWeights,
        Language = Lang.Current,
    };

    private void UpdateSliderLabels()
    {
        varietyValue.Text = varietyBar.Value.ToString(CultureInfo.CurrentCulture);
        correctionValue.Text = $"{PercentSteps.ToPercent(correctionBar.Value)}%";
        overlayValue.Text = $"{PercentSteps.ToPercent(overlayBar.Value)}%";
        edgesValue.Text = $"{PercentSteps.ToPercent(edgesBar.Value)}%";
    }

    private void UpdateOutputSize()
    {
        if (targetInfo is not { } info)
        {
            outputSizeLabel.Text = T(Texts.Main.OutputUnknown);
            outputSizeLabel.ForeColor = SystemColors.GrayText;
            return;
        }

        var settings = CurrentSettings();
        var (width, height, rows) = settings.OutputSize(info.Width, info.Height);
        var tooBig = (long)width * height > MosaicSettings.MaxOutputPixels;
        outputSizeLabel.Text =
            TF(Texts.Main.OutputTiles, settings.Columns, rows, settings.Columns * rows) + "\n" +
            TF(Texts.Main.OutputSize, width, height, (double)width * height / 1_000_000) +
            (tooBig ? T(Texts.Main.OutputTooBig) : "");
        outputSizeLabel.ForeColor = tooBig ? Color.Firebrick : SystemColors.GrayText;
    }

    // Choosing inputs

    private void BrowseTarget()
    {
        using var dialog = new OpenFileDialog
        {
            Title = T(Texts.Dialog.ChooseTarget),
            Filter = T(Texts.Dialog.Images) + "|" + string.Join(";", ImageCodec.SupportedExtensions.Select(e => "*" + e)) +
                     "|" + T(Texts.Dialog.AllFiles) + "|*.*",
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

    private void BrowseFolders()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = T(Texts.Dialog.Folders),
            UseDescriptionForTitle = true,
            Multiselect = true,
        };
        var last = TileFolders().LastOrDefault();
        if (last != null && Directory.Exists(last))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(last) ?? last;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddFolders(dialog.SelectedPaths);
        }
    }

    private List<string> TileFolders() => foldersList.Items.Cast<string>().ToList();

    private void AddFolders(IEnumerable<string> folders)
    {
        var added = false;
        foreach (var folder in folders.Select(NormalizePath).Where(f => f.Length > 0))
        {
            if (!TileFolders().Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                foldersList.Items.Add(folder);
                added = true;
            }
        }

        if (added)
        {
            CountTiles();
        }

        UpdateState();
    }

    private void RemoveSelectedFolders()
    {
        if (building || foldersList.SelectedIndices.Count == 0)
        {
            return;
        }

        var first = foldersList.SelectedIndices[0];
        foreach (var index in foldersList.SelectedIndices.Cast<int>().OrderDescending())
        {
            foldersList.Items.RemoveAt(index);
        }

        if (foldersList.Items.Count > 0)
        {
            foldersList.SelectedIndex = Math.Min(first, foldersList.Items.Count - 1);
        }

        CountTiles();
        UpdateState();
    }

    private void SuggestTarget()
    {
        var settings = CurrentSettings();
        var errors = settings.ValidateTilesFolders();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SuggestionsForm(builder, settings, suggestionAnalysis);
        var accepted = dialog.ShowDialog(this) == DialogResult.OK;
        suggestionWeights = dialog.Weights;
        suggestionAnalysis = dialog.Analysis;
        SettingsStore.Save(CurrentSettings());
        if (accepted && dialog.SelectedPath != null)
        {
            ShowTarget(dialog.SelectedPath);
        }

        UpdateCacheUsage();
    }

    // Tile cache

    private void UpdateCacheUsage()
    {
        var (files, bytes) = (0, 0L);
        try
        {
            (files, bytes) = new TileCache(TileCache.DefaultDirectory).GetUsage();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Unknown size: the link still works.
        }

        clearCacheLink.Text = files == 0 ? T(Texts.Main.CacheEmpty) : TF(Texts.Main.CacheClear, FormatSize(bytes));
        clearCacheLink.Enabled = files > 0 && !building;
    }

    private void ClearCache()
    {
        var cache = new TileCache(TileCache.DefaultDirectory);
        var (files, bytes) = cache.GetUsage();
        if (building || files == 0)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            TF(Texts.Main.CacheConfirm, files, FormatSize(bytes)),
            T(Texts.Main.CacheConfirmTitle),
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.OK)
        {
            return;
        }

        var (deleted, failed) = cache.Clear();
        SetStatus(() => TF(Texts.Main.CacheCleared, deleted, FormatSize(bytes)) + (failed > 0 ? TF(Texts.Main.CacheInUse, failed) : ""));
        UpdateCacheUsage();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 * 1024 => TF(Texts.Size.Kb, Math.Max(1, bytes / 1024)),
        < 1024L * 1024 * 1024 => TF(Texts.Size.Mb, bytes / (1024.0 * 1024)),
        _ => TF(Texts.Size.Gb, bytes / (1024.0 * 1024 * 1024)),
    };

    // Target and folders

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
            EndFragment();
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
                var name = Path.GetFileName(path);
                SetStatus(() => TF(Texts.Status.CannotRead, name, e.Message));
            }
        }

        UpdateOutputSize();
        UpdateState();
    }

    private async void CountTiles()
    {
        var folders = TileFolders();
        var version = ++scanVersion;
        if (folders.Count == 0)
        {
            SetTilesInfo(() => T(Texts.Tiles.Add));
            return;
        }

        var missing = folders.Where(f => !Directory.Exists(f)).ToList();
        if (missing.Count > 0)
        {
            SetTilesInfo(() => TF(Texts.Tiles.NotFound, missing[0]));
            return;
        }

        SetTilesInfo(() => T(Texts.Tiles.Counting));
        try
        {
            // A folder and its subfolder may both be in the list.
            var count = await Task.Run(() => folders
                .SelectMany(f => ImageFileScanner.Scan(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
            if (version == scanVersion)
            {
                SetTilesInfo(() => count == 0
                    ? T(Texts.Tiles.None)
                    : (folders.Count == 1 ? TF(Texts.Tiles.Found, count) : TF(Texts.Tiles.FoundFolders, count, folders.Count)) +
                      (count < 100 ? T(Texts.Tiles.Few) : "."));
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            if (version == scanVersion)
            {
                SetTilesInfo(() => e.Message);
            }
        }
    }

    // Building

    private async Task BuildOrCancelAsync()
    {
        if (building)
        {
            buildCts?.Cancel();
            SetStatus(() => T(Texts.Status.Cancelling));
            return;
        }

        var settings = CurrentSettings();
        var errors = settings.Validate();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        EndFragment();
        SettingsStore.Save(settings);
        rerenderTimer.Stop();
        rerenderCts?.Cancel();
        using var cts = new CancellationTokenSource();
        buildCts = cts;
        building = true;
        originalToggle.Checked = false;
        UpdateState();

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
            var stats = built.Library.Stats;
            var usable = built.Library.Tiles.Count;
            SetTilesInfo(() => TF(Texts.Tiles.Usable, usable, stats.Duplicates, stats.Failed));
            SetStatus(() => built.Summary);
            if (CurrentSettings().ToRenderOptions() != settings.ToRenderOptions())
            {
                // Look sliders were moved while building.
                rerenderTimer.Start();
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus(() => T(Texts.Status.Cancelled));
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            SetStatus(() => e.Message);
            MessageBox.Show(this, e.Message, "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception e)
        {
            SetStatus(() => TF(Texts.Status.Failed, e.Message));
            MessageBox.Show(this, e.ToString(), T(Texts.Error.Title), MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private void LookChanged()
    {
        UpdateSliderLabels();
        if (result != null && !building)
        {
            rerenderTimer.Stop();
            rerenderTimer.Start();
        }
    }

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
        var options = CurrentSettings().ToRenderOptions();
        try
        {
            var (updated, display) = await Task.Run(() =>
            {
                var r = MosaicBuilder.Rerender(current, options, null, cts.Token);
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

    // Saving

    private static string TimestampedName(string target, string kind) =>
        $"{Path.GetFileNameWithoutExtension(target)}_{kind}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.jpg";

    private string? AskSavePath(string title, string fileName, string target)
    {
        using var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = T(Texts.Dialog.ImageFilter),
            FileName = fileName,
            InitialDirectory = Path.GetDirectoryName(target),
            OverwritePrompt = true,
        };
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private async Task SaveAsync()
    {
        if (result is not { } current || building)
        {
            return;
        }

        var target = current.Settings.TargetImagePath;
        var path = AskSavePath(T(Texts.Dialog.Save), TimestampedName(target, "mosaic"), target);
        if (path == null)
        {
            return;
        }

        await SaveImageAsync(path, () => current.Image);
    }

    private async Task FragmentButtonAsync()
    {
        if (viewer.Frame != null)
        {
            await SaveFragmentAsync();
        }
        else
        {
            BeginFragment();
        }
    }

    private void BeginFragment()
    {
        if (result is not { } current || building)
        {
            return;
        }

        originalToggle.Checked = false;
        viewer.BeginFrame(current.Plan.Columns, current.Plan.Rows, current.Library.TileSize);
        ApplyFrameMode();
        viewer.Focus();
    }

    private void EndFragment()
    {
        viewer.EndFrame();
        ApplyFrameMode();
    }

    private void ApplyFrameMode()
    {
        var on = viewer.Frame != null;
        frameSaveButton.Visible = on;
        frameCancelButton.Visible = on;
        hintLabel.Text = on ? T(Texts.Main.FragmentHint) : T(Texts.Main.ViewHint);
        toolTip.SetToolTip(hintLabel, Localizer.Wrap(hintLabel.Text));
        UpdateState();
        if (!on && result != null && !building)
        {
            SetStatus(() => result?.Summary ?? "");
        }
    }

    // A smaller frame gets bigger tiles, up to MaxFragmentTileScale times the mosaic's, so the fragment is at most as wide as the mosaic.
    private static int FragmentTileSize(MosaicResult current, CellRect frame) =>
        Math.Clamp(
            (int)Math.Round((double)current.Image.Width / frame.Columns),
            current.Library.TileSize,
            Math.Max(current.Library.TileSize, Math.Min(current.Library.TileSize * MaxFragmentTileScale, MaxFragmentTileSize)));

    private void ShowFragmentInfo()
    {
        if (viewer.Frame is not { } frame || result is not { } current)
        {
            return;
        }

        var size = FragmentTileSize(current, frame);
        SetStatus(() => TF(Texts.Main.FragmentInfo, frame.Columns, frame.Rows, frame.Columns * size, frame.Rows * size, size));
    }

    private async Task SaveFragmentAsync()
    {
        if (viewer.Frame is not { } frame || result is not { } current || building || savingFragment)
        {
            return;
        }

        var target = current.Settings.TargetImagePath;
        var size = FragmentTileSize(current, frame);
        var path = AskSavePath(
            TF(Texts.Dialog.SaveFragment, frame.Columns * size, frame.Rows * size),
            TimestampedName(target, $"fragment_{frame.Columns}x{frame.Rows}"),
            target);
        if (path == null)
        {
            return;
        }

        savingFragment = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        SetStatus(() => T(Texts.Status.RenderingFragment));
        UpdateState();
        try
        {
            var saved = await SaveImageAsync(path, () => MosaicBuilder.RenderFragment(current, frame, size));
            if (saved)
            {
                EndFragment();
            }
        }
        finally
        {
            savingFragment = false;
            progressBar.Style = ProgressBarStyle.Continuous;
            UpdateState();
        }
    }

    private async Task<bool> SaveImageAsync(string path, Func<PixelBuffer> image)
    {
        SetStatus(() => T(Texts.Status.Saving));
        saveButton.Enabled = false;
        try
        {
            await Task.Run(() => ImageCodec.Save(image(), path));
            lastSavedPath = path;
            var name = Path.GetFileName(path);
            SetStatus(() => TF(Texts.Status.Saved, name));
            savedLinks.Visible = true;
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            SetStatus(() => TF(Texts.Status.NotSaved, e.Message));
            MessageBox.Show(this, e.Message, "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            UpdateState();
        }
    }

    private void OpenSaved(bool select)
    {
        if (lastSavedPath == null || !File.Exists(lastSavedPath))
        {
            savedLinks.Visible = false;
            return;
        }

        var start = select
            ? new ProcessStartInfo("explorer.exe", $"/select,\"{lastSavedPath}\"")
            : new ProcessStartInfo(lastSavedPath) { UseShellExecute = true };
        try
        {
            Process.Start(start)?.Dispose();
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, e.Message, "Image Mosaic", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // UI state

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

        SetStatus(p.ToString);
    }

    private void UpdateState()
    {
        var busy = building || savingFragment;
        buildButton.Text = building ? T(Texts.Main.Cancel) : T(Texts.Main.Build);
        buildButton.Enabled = !savingFragment;
        saveButton.Enabled = !busy && result != null;
        saveFragmentButton.Enabled = !busy && result != null && viewer.HasMosaic;
        saveFragmentButton.Text = viewer.Frame != null ? T(Texts.Main.FragmentSave) : T(Texts.Main.SaveFragment);
        frameSaveButton.Enabled = !busy;
        originalToggle.Enabled = result != null && viewer.HasMosaic && viewer.Frame == null;
        removeFolderButton.Enabled = !busy && foldersList.SelectedIndices.Count > 0;
        sourceGroup.Enabled = !busy;
        layoutGroup.Enabled = !busy;
        matchingGroup.Enabled = !busy;
        UpdateCacheUsage();
    }

    private void SetStatus(Func<string> text)
    {
        statusText = text;
        statusLabel.Text = text();
    }

    private void SetTilesInfo(Func<string> text)
    {
        tilesInfoText = text;
        tilesInfoLabel.Text = text();
    }

    // Paths pasted from Explorer come in quotes.
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

    // Keyboard and drag & drop

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
        var framing = viewer.Frame != null;
        switch (e.KeyCode)
        {
            case Keys.Enter when framing && !IsTyping():
                e.SuppressKeyPress = true;
                _ = SaveFragmentAsync();
                break;
            case Keys.Escape when framing:
                e.Handled = true;
                EndFragment();
                break;
            case Keys.F5 when !building:
                e.Handled = true;
                _ = BuildOrCancelAsync();
                break;
            case Keys.Escape when building:
                e.Handled = true;
                buildCts?.Cancel();
                break;
            case Keys.S when e.Control && e.Shift:
                e.SuppressKeyPress = true;
                _ = FragmentButtonAsync();
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

    private bool IsTyping() =>
        ActiveControl is TextBoxBase or NumericUpDown or ComboBox || ActiveControl?.Parent is NumericUpDown;

    private bool IsSpaceForViewer() => viewer.HasMosaic && viewer.Frame == null && !IsTyping();

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

        AddFolders(paths.Where(Directory.Exists));
        var image = paths.FirstOrDefault(p => File.Exists(p) && ImageCodec.IsSupported(p));
        if (image != null)
        {
            ShowTarget(image);
        }
    }
}
