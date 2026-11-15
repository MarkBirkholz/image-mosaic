using ImageMosaic.Controls;
using ImageMosaic.Core;

namespace ImageMosaic;

/// <summary>Creates the controls. The layout is built in code with table panels so it scales with DPI and window size.</summary>
internal sealed partial class MainForm
{
    private const int SidebarWidth = 340;

    private readonly ToolTip toolTip = new() { AutoPopDelay = 15000 };

    private TextBox targetBox = null!;
    private Button targetBrowse = null!;
    private TextBox tilesBox = null!;
    private Button tilesBrowse = null!;
    private Label tilesInfoLabel = null!;
    private NumericUpDown columnsInput = null!;
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
    private Button buildButton = null!;
    private Button saveButton = null!;
    private ProgressBar progressBar = null!;
    private Label statusLabel = null!;
    private GroupBox sourceGroup = null!;
    private GroupBox layoutGroup = null!;
    private GroupBox matchingGroup = null!;
    private ZoomableImageView viewer = null!;
    private CheckBox originalToggle = null!;
    private Button fitButton = null!;
    private Button actualSizeButton = null!;
    private Label zoomLabel = null!;
    private TextBox logBox = null!;
    private SplitContainer split = null!;

    private void BuildLayout()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = new Font("Segoe UI", 9F);
        Text = "Image Mosaic";
        ClientSize = new Size(1320, 840);
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
            Padding = new Padding(10, 8, 6, 8),
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // --- Images ---
        targetBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Picture to reproduce" };
        targetBrowse = new Button { Text = "Browse…", AutoSize = true };
        tilesBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Folder with your photos (subfolders included)" };
        tilesBrowse = new Button { Text = "Browse…", AutoSize = true };
        tilesInfoLabel = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 2, 3, 0) };
        var source = Grid(2);
        source.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        source.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddSpanning(source, Caption("Target image"));
        source.Controls.Add(targetBox);
        source.Controls.Add(targetBrowse);
        AddSpanning(source, Caption("Tile photos"));
        source.Controls.Add(tilesBox);
        source.Controls.Add(tilesBrowse);
        AddSpanning(source, tilesInfoLabel);
        sourceGroup = Group("1. Images", source);

        // --- Layout ---
        columnsInput = Number(MosaicSettings.MinColumns, MosaicSettings.MaxColumns, 80);
        tileSizeInput = Number(MosaicSettings.MinTileSize, MosaicSettings.MaxTileSize, 32);
        outputSizeLabel = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 4, 3, 0) };
        var layout = Grid(2);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(layout, "Tiles across", columnsInput,
            "Number of photos in one row. More columns = more detail of the target picture.");
        AddRow(layout, "Tile size, px", tileSizeInput,
            "Size of each photo in the saved image. Bigger tiles = photos easier to recognize up close, bigger file.");
        AddSpanning(layout, outputSizeLabel);
        layoutGroup = Group("2. Size", layout);

        // --- Matching ---
        gridInput = Number(1, 6, 3);
        repeatInput = Number(0, 20, 4);
        (varietyBar, varietyValue) = Slider(0, 10, 3);
        var matching = Grid(2);
        matching.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        matching.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(matching, "Color detail", gridInput,
            "Each photo and each cell is compared as an N×N grid of average colors.\n" +
            "1 = only the average color; 3 = also where it is lighter or darker (recommended).");
        AddRow(matching, "No repeats within, cells", repeatInput,
            "The same photo is never placed closer than this to itself. 0 allows neighbours to repeat.");
        AddSliderRows(matching, "Variety", varietyBar, varietyValue,
            "How hard to avoid using the same photos often.\n0 = always the closest color, 10 = spread over as many photos as possible.");
        matchingGroup = Group("3. Matching", matching);

        // --- Colors ---
        (correctionBar, correctionValue) = Slider(0, 100, 40);
        (overlayBar, overlayValue) = Slider(0, 100, 20);
        var colors = Grid(2);
        colors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        colors.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddSliderRows(colors, "Color correction", correctionBar, correctionValue,
            "Shifts each photo's colors toward the color of its cell. Keeps the photo's details.");
        AddSliderRows(colors, "Overlay", overlayBar, overlayValue,
            "Blends the target picture over the photos. Makes the picture clearer, the photos less visible.");
        var colorsHint = new Label
        {
            Text = "Applied instantly to a finished mosaic.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
        };
        AddSpanning(colors, colorsHint);
        var colorsGroup = Group("4. Colors", colors);

        // --- Actions ---
        buildButton = new Button
        {
            Text = "Build mosaic",
            Dock = DockStyle.Fill,
            Height = 40,
            Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold),
            Margin = new Padding(3, 8, 3, 3),
        };
        saveButton = new Button { Text = "Save as…", Dock = DockStyle.Fill, Height = 32, Enabled = false };
        progressBar = new ProgressBar { Dock = DockStyle.Fill, Maximum = 1000, Height = 8, Margin = new Padding(3, 8, 3, 0) };
        statusLabel = new Label { AutoSize = true, MaximumSize = new Size(SidebarWidth - 30, 0), Margin = new Padding(3, 4, 3, 0) };
        toolTip.SetToolTip(buildButton, "F5 — build, Esc — cancel");
        toolTip.SetToolTip(saveButton, "Ctrl+S");

        stack.Controls.Add(sourceGroup);
        stack.Controls.Add(layoutGroup);
        stack.Controls.Add(matchingGroup);
        stack.Controls.Add(colorsGroup);
        stack.Controls.Add(buildButton);
        stack.Controls.Add(saveButton);
        stack.Controls.Add(progressBar);
        stack.Controls.Add(statusLabel);

        var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        scroller.Controls.Add(stack);
        return scroller;
    }

    private Control BuildWorkspace()
    {
        originalToggle = new CheckBox
        {
            Text = "Show original",
            Appearance = Appearance.Button,
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(0, 0, 6, 0),
        };
        fitButton = new Button { Text = "Fit", AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        actualSizeButton = new Button { Text = "1:1", AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
        zoomLabel = new Label { AutoSize = true, Margin = new Padding(6, 7, 12, 0), ForeColor = SystemColors.GrayText };
        var hint = new Label
        {
            Text = "Wheel — zoom · drag — move · double-click — fit / 1:1 · Space — hold to see the original",
            AutoSize = true,
            Margin = new Padding(6, 7, 0, 0),
            ForeColor = SystemColors.GrayText,
        };
        toolTip.SetToolTip(originalToggle, "Compare with the target picture (or hold Space)");

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(4, 8, 4, 6),
        };
        toolbar.Controls.AddRange([originalToggle, fitButton, actualSizeButton, zoomLabel, hint]);

        viewer = new ZoomableImageView
        {
            Dock = DockStyle.Fill,
            EmptyText = "Choose a target picture and a folder of photos, then press “Build mosaic”.\n\n" +
                        "You can also drop an image or a folder onto this window.",
        };

        logBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9F),
            BorderStyle = BorderStyle.None,
            BackColor = SystemColors.Window,
        };

        split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            FixedPanel = FixedPanel.Panel2,
            Panel2MinSize = 40,
        };
        split.Panel1.Controls.Add(viewer);
        split.Panel2.Controls.Add(logBox);

        var workspace = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        workspace.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(toolbar, 0, 0);
        workspace.Controls.Add(split, 0, 1);
        return workspace;
    }

    private static TableLayoutPanel Grid(int columns) => new()
    {
        ColumnCount = columns,
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = Padding.Empty,
    };

    private static GroupBox Group(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 6, 8, 8),
            Margin = new Padding(0, 0, 0, 8),
        };
        content.Dock = DockStyle.Fill;
        group.Controls.Add(content);
        return group;
    }

    private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 6, 3, 2) };

    private static void AddSpanning(TableLayoutPanel grid, Control control)
    {
        grid.Controls.Add(control);
        grid.SetColumnSpan(control, grid.ColumnCount);
    }

    private void AddRow(TableLayoutPanel grid, string caption, Control input, string help)
    {
        var label = new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) };
        grid.Controls.Add(label);
        grid.Controls.Add(input);
        toolTip.SetToolTip(label, help);
        toolTip.SetToolTip(input, help);
    }

    private void AddSliderRows(TableLayoutPanel grid, string caption, TrackBar bar, Label value, string help)
    {
        var label = Caption(caption);
        value.Anchor = AnchorStyles.Right;
        grid.Controls.Add(label);
        grid.Controls.Add(value);
        AddSpanning(grid, bar);
        toolTip.SetToolTip(label, help);
        toolTip.SetToolTip(bar, help);
    }

    private static NumericUpDown Number(int min, int max, int value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 72,
        TextAlign = HorizontalAlignment.Right,
        Anchor = AnchorStyles.Right,
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
            Height = 28,
            SmallChange = 1,
            LargeChange = Math.Max(1, (max - min) / 10),
        };
        var label = new Label { AutoSize = true, Margin = new Padding(3, 6, 3, 2), ForeColor = SystemColors.GrayText };
        return (bar, label);
    }
}
