using ImageMosaic.Controls;
using ImageMosaic.Core;
using ImageMosaic.Infrastructure;

namespace ImageMosaic;

internal sealed partial class MainForm
{
    private const int SidebarWidth = 340;

    // Without a maximum width, a wrapping label is measured as one line and gets clipped.
    private static readonly Size WrapWidth = new(SidebarWidth - 60, 0);

    private readonly ToolTip toolTip = new() { AutoPopDelay = 30000 };
    private Localizer localizer = null!;

    private TextBox targetBox = null!;
    private Button targetBrowse = null!;
    private ListBox foldersList = null!;
    private Button addFolderButton = null!;
    private Button removeFolderButton = null!;
    private LinkLabel suggestLink = null!;
    private LinkLabel clearCacheLink = null!;
    private Panel settingsScroller = null!;
    private Label tilesInfoLabel = null!;
    private NumericUpDown longSideInput = null!;
    private NumericUpDown tileSizeInput = null!;
    private Label outputSizeLabel = null!;
    private NumericUpDown gridInput = null!;
    private NumericUpDown repeatInput = null!;
    private TrackBar varietyBar = null!;
    private Label varietyValue = null!;
    private TrackBar correctionBar = null!;
    private Label correctionValue = null!;
    private TrackBar overlayBar = null!;
    private Label overlayValue = null!;
    private TrackBar edgesBar = null!;
    private Label edgesValue = null!;
    private Button buildButton = null!;
    private Button saveButton = null!;
    private Button saveFragmentButton = null!;
    private ProgressBar progressBar = null!;
    private Label statusLabel = null!;
    private FlowLayoutPanel savedLinks = null!;
    private LinkLabel openSavedLink = null!;
    private LinkLabel showSavedLink = null!;
    private GroupBox sourceGroup = null!;
    private GroupBox layoutGroup = null!;
    private GroupBox matchingGroup = null!;
    private ZoomableImageView viewer = null!;
    private CheckBox originalToggle = null!;
    private Button fitButton = null!;
    private Button actualSizeButton = null!;
    private Button frameSaveButton = null!;
    private Button frameCancelButton = null!;
    private Label zoomLabel = null!;
    private Label hintLabel = null!;
    private ComboBox languageBox = null!;

    private void BuildLayout()
    {
        SuspendLayout();
        localizer = new Localizer(toolTip);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = new Font("Segoe UI", 9F);
        Text = "Image Mosaic";
        ClientSize = new Size(1320, 920);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        AllowDrop = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildWorkspace(), 1, 0);
        Controls.Add(root);
        ResumeLayout(true);
    }

    private Control BuildSidebar()
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(8, 6, 4, 4),
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Images
        foldersList = new ListBox
        {
            Dock = DockStyle.Fill,
            Height = 64,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            SelectionMode = SelectionMode.MultiExtended,
        };
        addFolderButton = SmallButton("+");
        removeFolderButton = SmallButton("−");
        var folderButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty,
        };
        folderButtons.Controls.AddRange([addFolderButton, removeFolderButton]);
        tilesInfoLabel = new Label { AutoSize = true, MaximumSize = WrapWidth, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 2, 3, 0) };
        targetBox = localizer.Placeholder(new TextBox { Dock = DockStyle.Fill }, Texts.Main.TargetPlaceholder);
        targetBrowse = localizer.Text(new Button { AutoSize = true }, Texts.Main.Browse);
        suggestLink = localizer.Text(
            new LinkLabel { AutoSize = true, MaximumSize = WrapWidth, Margin = new Padding(3, 2, 3, 0) },
            Texts.Main.Suggest);
        var source = Grid(2);
        source.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        source.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddSpanning(source, Caption(Texts.Main.Folders));
        source.Controls.Add(foldersList);
        source.Controls.Add(folderButtons);
        AddSpanning(source, tilesInfoLabel);
        AddSpanning(source, Caption(Texts.Main.Target));
        source.Controls.Add(targetBox);
        source.Controls.Add(targetBrowse);
        AddSpanning(source, suggestLink);
        sourceGroup = Group(Texts.Main.GroupImages, source);
        localizer.Tip(foldersList, Texts.Main.FoldersTip);
        localizer.Tip(addFolderButton, Texts.Main.AddFolderTip);
        localizer.Tip(removeFolderButton, Texts.Main.RemoveFolderTip);
        localizer.Tip(suggestLink, Texts.Main.SuggestTip);

        // Size
        longSideInput = Number(MosaicSettings.MinLongSideTiles, MosaicSettings.MaxLongSideTiles, 80);
        tileSizeInput = Number(MosaicSettings.MinTileSize, MosaicSettings.MaxTileSize, 32);
        outputSizeLabel = new Label { AutoSize = true, MaximumSize = WrapWidth, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 2, 3, 0) };
        var layout = Grid(3);
        AddRow(layout, longSideInput, Texts.Main.LongSide, Texts.Main.LongSideTip);
        AddRow(layout, tileSizeInput, Texts.Main.TileSize, Texts.Main.TileSizeTip);
        AddSpanning(layout, outputSizeLabel);
        layoutGroup = Group(Texts.Main.GroupSize, layout);

        // Matching
        gridInput = Number(1, 6, 3);
        repeatInput = Number(0, 20, 4);
        (varietyBar, varietyValue) = Slider(0, 10, 3);
        var matching = Grid(3);
        AddRow(matching, gridInput, Texts.Main.Grid, Texts.Main.GridTip);
        AddRow(matching, repeatInput, Texts.Main.Repeat, Texts.Main.RepeatTip);
        AddSliderRow(matching, varietyBar, varietyValue, Texts.Main.Variety, Texts.Main.VarietyTip);
        matchingGroup = Group(Texts.Main.GroupMatching, matching);

        // Look
        (correctionBar, correctionValue) = Slider(0, PercentSteps.Count, PercentSteps.ToSteps(0.4));
        (overlayBar, overlayValue) = Slider(0, PercentSteps.Count, PercentSteps.ToSteps(0.2));
        (edgesBar, edgesValue) = Slider(0, (int)Math.Round(MosaicSettings.MaxEdgeBlend * 100), 0);
        var look = Grid(3);
        AddSliderRow(look, correctionBar, correctionValue, Texts.Main.Correction, Texts.Main.CorrectionTip);
        AddSliderRow(look, overlayBar, overlayValue, Texts.Main.Overlay, Texts.Main.OverlayTip);
        AddSliderRow(look, edgesBar, edgesValue, Texts.Main.Edges, Texts.Main.EdgesTip);
        var lookGroup = Group(Texts.Main.GroupLook, look);
        localizer.Tip(lookGroup, Texts.Main.LookHint);

        // Build and save
        buildButton = new Button
        {
            Dock = DockStyle.Fill,
            Height = 36,
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold),
            Margin = new Padding(3, 2, 3, 10),
        };
        saveButton = localizer.Text(new Button { Dock = DockStyle.Fill, Height = 32, Enabled = false }, Texts.Main.Save);
        saveFragmentButton = localizer.Text(new Button { Dock = DockStyle.Fill, Height = 32, Enabled = false }, Texts.Main.SaveFragment);
        var saveButtons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
        saveButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        saveButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        saveButtons.Controls.Add(saveButton, 0, 0);
        saveButtons.Controls.Add(saveFragmentButton, 1, 0);
        progressBar = new ProgressBar { Dock = DockStyle.Fill, Maximum = 1000, Height = 8, Margin = new Padding(3, 8, 3, 0) };
        statusLabel = new Label { AutoSize = true, MaximumSize = new Size(SidebarWidth - 30, 0), Margin = new Padding(3, 4, 3, 0) };
        openSavedLink = localizer.Text(new LinkLabel { AutoSize = true, Margin = new Padding(3, 2, 6, 0) }, Texts.Saved.Open);
        showSavedLink = localizer.Text(new LinkLabel { AutoSize = true, Margin = new Padding(3, 2, 3, 0) }, Texts.Saved.Folder);
        savedLinks = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty, Visible = false };
        savedLinks.Controls.AddRange([openSavedLink, showSavedLink]);
        clearCacheLink = new LinkLabel { AutoSize = true, MaximumSize = WrapWidth, Margin = new Padding(3, 8, 3, 0) };
        localizer.Tip(buildButton, Texts.Main.BuildTip);
        localizer.Tip(saveButton, Texts.Main.SaveTip);
        localizer.Tip(saveFragmentButton, Texts.Main.SaveFragmentTip);
        localizer.Tip(clearCacheLink, Texts.Main.CacheTip);

        stack.Controls.Add(sourceGroup);
        stack.Controls.Add(layoutGroup);
        // Build right after the settings it uses; the look below is applied to the finished mosaic.
        stack.Controls.Add(matchingGroup);
        stack.Controls.Add(buildButton);
        stack.Controls.Add(lookGroup);

        // Settings scroll, saving and the status stay at the bottom.
        var scroller = settingsScroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        scroller.Controls.Add(stack);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(8, 0, 4, 6),
            Margin = Padding.Empty,
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.Controls.Add(saveButtons, 0, 0);
        actions.Controls.Add(progressBar, 0, 1);
        actions.Controls.Add(statusLabel, 0, 2);
        actions.Controls.Add(savedLinks, 0, 3);
        actions.Controls.Add(clearCacheLink, 0, 4);

        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebar.Controls.Add(scroller, 0, 0);
        sidebar.Controls.Add(actions, 0, 1);
        return sidebar;
    }

    private Control BuildWorkspace()
    {
        var gap = new Padding(0, 0, 6, 0);
        originalToggle = localizer.Text(
            new CheckBox { Appearance = Appearance.Button, AutoSize = true, Enabled = false, Margin = gap },
            Texts.Main.Original);
        fitButton = localizer.Text(new Button { AutoSize = true, Margin = gap }, Texts.Main.Fit);
        actualSizeButton = new Button { Text = "1:1", AutoSize = true, Margin = gap };
        frameSaveButton = localizer.Text(
            new Button { AutoSize = true, Margin = gap, Visible = false, Font = new Font(Font, FontStyle.Bold) },
            Texts.Main.FragmentSave);
        frameCancelButton = localizer.Text(new Button { AutoSize = true, Margin = gap, Visible = false }, Texts.Main.FragmentCancel);
        zoomLabel = new Label { AutoSize = true, Margin = new Padding(6, 7, 12, 0), ForeColor = SystemColors.GrayText };
        localizer.Tip(originalToggle, Texts.Main.OriginalTip);

        var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        tools.Controls.AddRange([originalToggle, fitButton, actualSizeButton, frameSaveButton, frameCancelButton, zoomLabel]);

        // Ellipsis on narrow windows keeps the language box visible.
        hintLabel = new Label
        {
            AutoSize = false,
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 0, 0, 0),
            ForeColor = SystemColors.GrayText,
        };

        languageBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 110,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(6, 1, 0, 0),
            DisplayMember = nameof(Language.Name),
        };
        localizer.Tip(languageBox, Texts.Main.LanguageTip);

        var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, Padding = new Padding(4, 8, 8, 6) };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        toolbar.Controls.Add(tools, 0, 0);
        toolbar.Controls.Add(hintLabel, 1, 0);
        toolbar.Controls.Add(languageBox, 2, 0);

        viewer = localizer.On(new ZoomableImageView { Dock = DockStyle.Fill }, v =>
        {
            v.EmptyText = T(Texts.Main.Empty);
            v.Invalidate();
        });

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        workspace.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(toolbar, 0, 0);
        workspace.Controls.Add(viewer, 0, 1);
        return workspace;
    }

    /// <summary>With 3 columns: label, input or slider, value — one line per setting.</summary>
    private static TableLayoutPanel Grid(int columns)
    {
        var grid = new TableLayoutPanel
        {
            ColumnCount = columns,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
        };
        if (columns == 3)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        return grid;
    }

    private GroupBox Group(string key, Control content)
    {
        var group = new GroupBox
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6, 2, 6, 4),
            Margin = new Padding(0, 0, 0, 6),
        };
        content.Dock = DockStyle.Fill;
        group.Controls.Add(content);
        return localizer.Text(group, key);
    }

    private static Button SmallButton(string text) => new()
    {
        Text = text,
        Size = new Size(30, 28),
        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        Margin = new Padding(3, 0, 0, 4),
    };

    private Label Caption(string key) =>
        localizer.Text(new Label { AutoSize = true, Margin = new Padding(3, 4, 3, 2) }, key);

    private static void AddSpanning(TableLayoutPanel grid, Control control)
    {
        grid.Controls.Add(control);
        grid.SetColumnSpan(control, grid.ColumnCount);
    }

    private Label RowLabel(string key, string tipKey, Control input)
    {
        var label = localizer.Text(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 3, 3, 3) }, key);
        localizer.Tip(label, tipKey);
        localizer.Tip(input, tipKey);
        return label;
    }

    private void AddRow(TableLayoutPanel grid, Control input, string key, string tipKey)
    {
        var label = RowLabel(key, tipKey, input);
        grid.Controls.Add(label);
        grid.SetColumnSpan(label, grid.ColumnCount - 1);
        grid.Controls.Add(input);
    }

    private void AddSliderRow(TableLayoutPanel grid, TrackBar bar, Label value, string key, string tipKey)
    {
        grid.Controls.Add(RowLabel(key, tipKey, bar));
        grid.Controls.Add(bar);
        grid.Controls.Add(value);
    }

    private static NumericUpDown Number(int min, int max, int value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 60,
        TextAlign = HorizontalAlignment.Right,
        Anchor = AnchorStyles.Right,
        Margin = new Padding(3, 2, 3, 2),
    };

    private static (TrackBar Bar, Label Value) Slider(int min, int max, int value)
    {
        var bar = new TrackBar
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            TickStyle = TickStyle.None,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 26,
            SmallChange = 1,
            LargeChange = Math.Max(1, (max - min) / 10),
            Margin = new Padding(0, 1, 0, 1),
        };
        var label = new Label
        {
            AutoSize = false,
            Size = new Size(40, 20),
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 3, 3, 3),
            ForeColor = SystemColors.GrayText,
        };
        return (bar, label);
    }
}
