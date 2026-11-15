using System.Globalization;
using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Controls;
using ImageMosaic.Core.Suggestions;
using ImageMosaic.Infrastructure;

namespace ImageMosaic;

internal sealed class SuggestionsForm : Form
{
    private const int CardWidth = 232;
    private const int ThumbWidth = 220;
    private const int ThumbHeight = 150;
    private const int WeightsWidth = 280;

    // Cards are added a page at a time as the list is scrolled down.
    private const int PageSize = 24;

    private static readonly Color SelectedColor = Color.FromArgb(204, 228, 247);

    private readonly MosaicBuilder builder;
    private readonly MosaicSettings settings;
    private readonly CancellationTokenSource cts = new();
    private readonly Dictionary<string, Bitmap> thumbnails = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> loadingThumbnails = new(StringComparer.OrdinalIgnoreCase);
    private readonly ToolTip toolTip = new() { AutoPopDelay = 30000 };
    private readonly List<ToolTip> balloons = [];
    private readonly System.Windows.Forms.Timer rerankTimer = new() { Interval = 120 };
    private readonly FlowLayoutPanel cards;
    private readonly Label statusLabel;
    private readonly ProgressBar progressBar;
    private readonly Button useButton;
    private readonly List<(TrackBar Bar, Label Value)> sliders = [];
    private readonly RangeSlider detailRange = new() { Dock = DockStyle.Fill, Step = PercentSteps.Step, Margin = new Padding(3, 1, 3, 1) };
    private readonly Label detailValue = new() { AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 6, 3, 0) };
    private IReadOnlyList<TargetCandidate> analyzed = [];
    private IReadOnlyList<TargetSuggestion> ranked = [];
    private Panel? selectedCard;
    private bool applyingWeights;
    private bool showMoreQueued;

    public SuggestionsForm(MosaicBuilder builder, MosaicSettings settings, SuggestionAnalysis? previous)
    {
        this.builder = builder;
        this.settings = settings;
        Analysis = previous;
        Weights = settings.SuggestionWeights ?? SuggestionWeights.Default;

        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = new Font("Segoe UI", 9F);
        Text = T(Texts.Suggest.Title);
        ClientSize = new Size(1330, 760);
        MinimumSize = new Size(840, 480);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;

        var intro = new Label { Text = T(Texts.Suggest.Intro), AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
        statusLabel = new Label { AutoSize = true, Margin = new Padding(3, 0, 3, 4), ForeColor = SystemColors.GrayText };
        progressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 8, Maximum = 1000, Style = ProgressBarStyle.Marquee };
        cards = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = SystemColors.Window, Padding = new Padding(6) };
        cards.Scroll += (_, _) => QueueShowMore();
        cards.MouseWheel += (_, _) => QueueShowMore();
        cards.Resize += (_, _) => QueueShowMore();
        useButton = new Button { Text = T(Texts.Suggest.Use), AutoSize = true, Enabled = false, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = T(Texts.Main.Cancel), AutoSize = true, DialogResult = DialogResult.Cancel };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 6, 0, 0) };
        buttons.Controls.AddRange([cancelButton, useButton]);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WeightsWidth));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        // Explicit cells: auto-placement skips hidden controls and would shift the rows.
        root.Controls.Add(intro, 0, 0);
        root.SetColumnSpan(intro, 2);
        root.Controls.Add(statusLabel, 0, 1);
        root.SetColumnSpan(statusLabel, 2);
        root.Controls.Add(progressBar, 0, 2);
        root.SetColumnSpan(progressBar, 2);
        root.Controls.Add(cards, 0, 3);
        root.Controls.Add(BuildWeightsPanel(), 1, 3);
        root.Controls.Add(buttons, 0, 4);
        root.SetColumnSpan(buttons, 2);
        Controls.Add(root);

        AcceptButton = useButton;
        CancelButton = cancelButton;
        rerankTimer.Tick += (_, _) =>
        {
            rerankTimer.Stop();
            Rerank();
        };
    }

    public string? SelectedPath { get; private set; }

    // Kept even when the dialog is cancelled.
    public SuggestionWeights Weights { get; private set; }

    // Passed to the next opening, so an unchanged library is not analyzed again.
    public SuggestionAnalysis? Analysis { get; private set; }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await AnalyzeAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        cts.Cancel();
        rerankTimer.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cts.Dispose();
            toolTip.Dispose();
            foreach (var balloon in balloons)
            {
                balloon.Dispose();
            }

            rerankTimer.Dispose();
            foreach (var bitmap in thumbnails.Values)
            {
                bitmap.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private static string NoteText(SuggestionNote note) => note switch
    {
        SuggestionNote.ColorsWellCovered => T(Texts.Note.ColorsWellCovered),
        SuggestionNote.FewPhotosInTheseColors => T(Texts.Note.FewPhotosInColors),
        SuggestionNote.ClearShapes => T(Texts.Note.ClearShapes),
        SuggestionNote.LowContrast => T(Texts.Note.LowContrast),
        SuggestionNote.Colorful => T(Texts.Note.Colorful),
        SuggestionNote.MostlyOneColor => T(Texts.Note.MostlyOneColor),
        SuggestionNote.LotsOfDetail => T(Texts.Note.LotsOfDetail),
        SuggestionNote.LittleDetail => T(Texts.Note.LittleDetail),
        _ => note.ToString(),
    };

    // Weights

    private Control BuildWeightsPanel()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 3 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddSlider(grid, Texts.Weight.ColorMatch, Texts.Weight.ColorMatchHelp);
        AddSlider(grid, Texts.Weight.ColorVariety, Texts.Weight.ColorVarietyHelp);
        AddSlider(grid, Texts.Weight.Contrast, Texts.Weight.ContrastHelp);

        var ratios = new Label
        {
            Text = T(Texts.Suggest.Ratios),
            AutoSize = true,
            MaximumSize = new Size(WeightsWidth - 60, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 0, 3, 12),
        };
        grid.Controls.Add(ratios);
        grid.SetColumnSpan(ratios, 2);
        grid.Controls.Add(InfoButton(Texts.Suggest.Score, Texts.Suggest.ScoreHelp));

        AddDetailRange(grid);

        var reset = new Button { Text = T(Texts.Suggest.Reset), AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
        reset.Click += (_, _) => ApplyWeights(SuggestionWeights.Default);
        grid.Controls.Add(reset);
        grid.SetColumnSpan(reset, 3);

        var group = new GroupBox { Text = T(Texts.Suggest.Weights), Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 8), Margin = new Padding(8, 0, 0, 0) };
        group.Controls.Add(grid);
        ApplyWeights(Weights);
        return group;
    }

    private void AddSlider(TableLayoutPanel grid, string key, string helpKey)
    {
        var label = new Label { Text = T(key), AutoSize = true, Margin = new Padding(3, 6, 3, 0) };
        var value = new Label { AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 6, 3, 0) };
        var bar = new TrackBar
        {
            Minimum = 0,
            Maximum = PercentSteps.Count,
            TickStyle = TickStyle.None,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 28,
            SmallChange = 1,
            LargeChange = 2,
        };
        bar.ValueChanged += (_, _) => WeightsChanged();
        grid.Controls.Add(label);
        grid.Controls.Add(value);
        grid.Controls.Add(InfoButton(key, helpKey));
        grid.Controls.Add(bar);
        grid.SetColumnSpan(bar, 3);
        sliders.Add((bar, value));
    }

    private void AddDetailRange(TableLayoutPanel grid)
    {
        var label = new Label { Text = T(Texts.Weight.Detail), AutoSize = true, Margin = new Padding(3, 6, 3, 0) };
        detailRange.ValueChanged += (_, _) => WeightsChanged();
        grid.Controls.Add(label);
        grid.Controls.Add(detailValue);
        grid.Controls.Add(InfoButton(Texts.Weight.Detail, Texts.Weight.DetailHelp));
        grid.Controls.Add(detailRange);
        grid.SetColumnSpan(detailRange, 3);
    }

    // A balloon, unlike a dialog, does not take focus: sliders stay usable while it is open.
    private Button InfoButton(string key, string helpKey)
    {
        var button = new Button
        {
            Text = "i",
            Size = new Size(22, 22),
            Font = new Font("Georgia", 9F, FontStyle.Bold | FontStyle.Italic),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(0, 102, 204),
            Margin = new Padding(2, 3, 0, 0),
            Anchor = AnchorStyles.Right,
            TabStop = false,
            Cursor = Cursors.Help,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0, 102, 204);

        var text = Localizer.Wrap(T(helpKey), 70);
        var balloon = new ToolTip
        {
            IsBalloon = true,
            ToolTipTitle = T(key),
            ToolTipIcon = ToolTipIcon.Info,
            AutoPopDelay = 32000,
            InitialDelay = 150,
        };
        balloon.SetToolTip(button, text);
        button.Click += (_, _) => balloon.Show(text, button, button.Width / 2, button.Height / 2, 30000);
        balloons.Add(balloon);
        return button;
    }

    private void ApplyWeights(SuggestionWeights weights)
    {
        applyingWeights = true;
        double[] values = [weights.ColorMatch, weights.ColorVariety, weights.Contrast];
        for (var i = 0; i < sliders.Count; i++)
        {
            sliders[i].Bar.Value = PercentSteps.ToSteps((int)Math.Round(values[i]));
        }

        detailRange.SetRange((int)Math.Round(weights.DetailFrom), (int)Math.Round(weights.DetailTo));
        applyingWeights = false;
        WeightsChanged();
    }

    private void WeightsChanged()
    {
        if (applyingWeights || sliders.Count < 3)
        {
            return;
        }

        int Value(int i) => PercentSteps.ToPercent(sliders[i].Bar.Value);
        for (var i = 0; i < sliders.Count; i++)
        {
            sliders[i].Value.Text = Value(i).ToString(CultureInfo.CurrentCulture);
        }

        Weights = new SuggestionWeights
        {
            ColorMatch = Value(0),
            ColorVariety = Value(1),
            Contrast = Value(2),
            DetailFrom = detailRange.Low,
            DetailTo = detailRange.High,
        };
        detailValue.Text = Weights.LimitsDetail ? $"{detailRange.Low}–{detailRange.High} %" : T(Texts.Suggest.DetailAll);
        rerankTimer.Stop();
        rerankTimer.Start();
    }

    // Analysis and ranking

    private async Task AnalyzeAsync()
    {
        var progress = new Progress<MosaicProgress>(p =>
        {
            statusLabel.Text = p.ToString();
            if (p.Stage is MosaicStage.LoadingTiles or MosaicStage.Analyzing && p.Total > 0)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = (int)(p.Fraction * progressBar.Maximum);
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
        });

        var options = new SuggestionOptions { Match = settings.ToMatchOptions() };
        try
        {
            var previous = Analysis;
            var analysis = await Task.Run(() =>
            {
                var library = builder.LoadLibrary(settings, progress, cts.Token);
                // Quick from the tile cache; tells whether photos or settings changed.
                if (previous != null && previous.IsValidFor(library, options))
                {
                    return previous;
                }

                return SuggestionAnalysis.Run(library, options, progress, cts.Token);
            });

            Analysis = analysis;
            analyzed = analysis.Candidates;
            Rerank();
        }
        catch (OperationCanceledException)
        {
            // Dialog closed while analyzing.
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            statusLabel.Text = ex.Message;
        }
        finally
        {
            if (!IsDisposed)
            {
                progressBar.Visible = false;
            }
        }
    }

    private void Rerank()
    {
        if (analyzed.Count == 0 || IsDisposed)
        {
            return;
        }

        ranked = TargetSuggester.Rank(analyzed, Weights, int.MaxValue);
        var previous = SelectedPath;

        cards.SuspendLayout();
        var old = cards.Controls.Cast<Control>().ToList();
        cards.Controls.Clear();
        foreach (var control in old)
        {
            control.Dispose(); // thumbnails are cached and disposed with the form
        }

        selectedCard = null;
        cards.ResumeLayout();
        cards.AutoScrollPosition = Point.Empty;
        ShowMore();

        var keep = cards.Controls.Cast<Panel>().FirstOrDefault(c => string.Equals((string)c.Tag!, previous, StringComparison.OrdinalIgnoreCase));
        if ((keep ?? cards.Controls.Cast<Panel>().FirstOrDefault()) is { } card)
        {
            SelectCard(card);
        }
        else
        {
            SelectedPath = null;
            useButton.Enabled = false;
        }

        statusLabel.Text = ranked.Count == 0
            ? T(Texts.Suggest.StatusNone)
            : (Weights.LimitsDetail ? TF(Texts.Suggest.StatusInRange, ranked.Count, analyzed.Count) : TF(Texts.Suggest.StatusAll, analyzed.Count)) +
              (Weights.AllZero ? T(Texts.Suggest.StatusAllZero) : "") +
              T(Texts.Suggest.StatusPick);
    }

    /// <summary>Adds the next page of cards.</summary>
    private void ShowMore()
    {
        var shown = cards.Controls.Count;
        if (shown >= ranked.Count)
        {
            return;
        }

        cards.SuspendLayout();
        foreach (var (suggestion, i) in ranked.Skip(shown).Take(PageSize).Select((s, i) => (s, shown + i)))
        {
            cards.Controls.Add(CreateCard(suggestion, i + 1));
        }

        cards.ResumeLayout(true);
        QueueShowMore();
    }

    /// <summary>Checks after the current layout and input are done, so the new cards' positions are known.</summary>
    private void QueueShowMore()
    {
        if (showMoreQueued || !IsHandleCreated || IsDisposed)
        {
            return;
        }

        showMoreQueued = true;
        BeginInvoke(() =>
        {
            showMoreQueued = false;
            ShowMoreIfNeeded();
        });
    }

    /// <summary>Adds a page when less than two rows of cards are left below the visible part of the list.</summary>
    private void ShowMoreIfNeeded()
    {
        if (IsDisposed || cards.Controls.Count == 0 || cards.Controls.Count >= ranked.Count)
        {
            return;
        }

        var below = cards.Controls[^1].Bottom - cards.ClientSize.Height;
        if (below < 2 * cards.Controls[^1].Height)
        {
            ShowMore();
        }
    }

    private Panel CreateCard(TargetSuggestion suggestion, int rank)
    {
        var card = new Panel
        {
            Width = CardWidth,
            Height = ThumbHeight + 92,
            Margin = new Padding(4),
            Padding = new Padding(6),
            Tag = suggestion.Path,
            Cursor = Cursors.Hand,
        };
        var picture = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Bounds = new Rectangle(6, 6, ThumbWidth, ThumbHeight),
            BackColor = Color.FromArgb(38, 38, 40),
        };
        var title = new Label
        {
            Text = $"#{rank}   {suggestion.Score.ToString("N0", CultureInfo.CurrentCulture)} / 100   " +
                   $"{suggestion.Width}×{suggestion.Height}",
            Font = new Font(Font, FontStyle.Bold),
            Bounds = new Rectangle(6, ThumbHeight + 10, ThumbWidth, 18),
        };
        var notes = new Label
        {
            Text = suggestion.Notes.Count > 0 ? string.Join(", ", suggestion.Notes.Select(NoteText)) : Path.GetFileName(suggestion.Path),
            ForeColor = SystemColors.GrayText,
            Bounds = new Rectangle(6, ThumbHeight + 28, ThumbWidth, 58),
        };
        card.Controls.AddRange([picture, title, notes]);

        var m = suggestion.Metrics;
        var p = suggestion.Parts;
        var details = $"{suggestion.Path}\n\n" + T(Texts.Suggest.Marks) + "\n" +
                      $"  {T(Texts.Weight.ColorMatch)}: {p.ColorMatch:P0} (ΔE {m.MeanError:N1})\n" +
                      $"  {T(Texts.Weight.ColorVariety)}: {p.ColorVariety:P0}\n" +
                      $"  {T(Texts.Weight.Contrast)}: {p.Contrast:P0}\n" +
                      $"  {T(Texts.Weight.Detail)}: {p.Detail:P0} (ΔE {m.Detail:N1})";
        foreach (Control control in new Control[] { card, picture, title, notes })
        {
            control.Click += (_, _) => SelectCard(card);
            control.DoubleClick += (_, _) =>
            {
                SelectCard(card);
                DialogResult = DialogResult.OK;
            };
            toolTip.SetToolTip(control, details);
        }

        ShowThumbnail(suggestion.Path, picture);
        return card;
    }

    private async void ShowThumbnail(string path, PictureBox picture)
    {
        if (thumbnails.TryGetValue(path, out var cached))
        {
            picture.Image = cached;
            return;
        }

        if (!loadingThumbnails.Add(path))
        {
            return; // already loading
        }

        try
        {
            var bitmap = await Task.Run(() => DisplayImage.ToBitmap(ImageCodec.Load(path, TargetSuggester.ThumbnailShortSide)));
            if (IsDisposed)
            {
                bitmap.Dispose();
                return;
            }

            thumbnails[path] = bitmap;
            // The card may have been replaced by a re-rank meanwhile.
            foreach (var card in cards.Controls.Cast<Panel>().Where(c => string.Equals((string)c.Tag!, path, StringComparison.OrdinalIgnoreCase)))
            {
                card.Controls.OfType<PictureBox>().First().Image = bitmap;
            }
        }
        catch (Exception e) when (e is IOException or NotSupportedException or UnauthorizedAccessException
                                      or System.Runtime.InteropServices.COMException or ArgumentException)
        {
            // Leave the dark placeholder.
        }
        finally
        {
            loadingThumbnails.Remove(path);
        }
    }

    private void SelectCard(Panel card)
    {
        if (selectedCard != null && !selectedCard.IsDisposed)
        {
            selectedCard.BackColor = SystemColors.Window;
        }

        selectedCard = card;
        card.BackColor = SelectedColor;
        SelectedPath = (string)card.Tag!;
        useButton.Enabled = true;
    }
}
