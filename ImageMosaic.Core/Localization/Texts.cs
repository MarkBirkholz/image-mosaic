namespace ImageMosaic.Core.Localization;

/// <summary>Text keys of the language files.</summary>
public static class Texts
{
    public static class Progress
    {
        public const string Scanning = "progress.scanning";
        public const string LoadingTiles = "progress.loading_tiles";
        public const string LoadingTarget = "progress.loading_target";
        public const string Matching = "progress.matching";
        public const string Rendering = "progress.rendering";
        public const string Analyzing = "progress.analyzing";
        public const string Done = "progress.done";
    }

    public static class Validation
    {
        public const string ChooseTarget = "validation.choose_target";
        public const string TargetNotFound = "validation.target_not_found";
        public const string UnsupportedType = "validation.unsupported_type";
        public const string ColumnsRange = "validation.columns_range";
        public const string TileSizeRange = "validation.tile_size_range";
        public const string GridRange = "validation.grid_range";
        public const string RepeatNegative = "validation.repeat_negative";
        public const string VarietyNegative = "validation.variety_negative";
        public const string ColorsRange = "validation.colors_range";
        public const string AddFolder = "validation.add_folder";
        public const string FolderNotFound = "validation.folder_not_found";
    }

    public static class Build
    {
        public const string Summary = "build.summary";
    }

    public static class Error
    {
        public const string TooBig = "error.too_big";
        public const string NoImages = "error.no_images";
        public const string Title = "error.title";
    }

    public static class Library
    {
        public const string Stats = "library.stats";
    }

    public static class Log
    {
        public const string Target = "log.target";
        public const string RepeatLowered = "log.repeat_lowered";
        public const string Matching = "log.matching";
        public const string Rendering = "log.rendering";
        public const string Done = "log.done";
        public const string Tiles = "log.tiles";
        public const string Skipped = "log.skipped";
        public const string MoreUnreadable = "log.more_unreadable";
        public const string CacheWriteFailed = "log.cache_write_failed";
    }

    public static class Main
    {
        public const string GroupImages = "main.group.images";
        public const string Folders = "main.folders";
        public const string FoldersTip = "main.folders.tip";
        public const string AddFolderTip = "main.add_folder.tip";
        public const string RemoveFolderTip = "main.remove_folder.tip";
        public const string Target = "main.target";
        public const string TargetPlaceholder = "main.target.placeholder";
        public const string Browse = "main.browse";
        public const string Suggest = "main.suggest";
        public const string SuggestTip = "main.suggest.tip";
        public const string GroupSize = "main.group.size";
        public const string Columns = "main.columns";
        public const string ColumnsTip = "main.columns.tip";
        public const string TileSize = "main.tile_size";
        public const string TileSizeTip = "main.tile_size.tip";
        public const string OutputUnknown = "main.output.unknown";
        public const string OutputTiles = "main.output.tiles";
        public const string OutputSize = "main.output.size";
        public const string OutputTooBig = "main.output.too_big";
        public const string GroupMatching = "main.group.matching";
        public const string Grid = "main.grid";
        public const string GridTip = "main.grid.tip";
        public const string Repeat = "main.repeat";
        public const string RepeatTip = "main.repeat.tip";
        public const string Variety = "main.variety";
        public const string VarietyTip = "main.variety.tip";
        public const string GroupLook = "main.group.look";
        public const string Correction = "main.correction";
        public const string CorrectionTip = "main.correction.tip";
        public const string Overlay = "main.overlay";
        public const string OverlayTip = "main.overlay.tip";
        public const string Edges = "main.edges";
        public const string EdgesTip = "main.edges.tip";
        public const string LookHint = "main.look.hint";
        public const string Build = "main.build";
        public const string BuildTip = "main.build.tip";
        public const string Cancel = "main.cancel";
        public const string Save = "main.save";
        public const string SaveTip = "main.save.tip";
        public const string SaveFragment = "main.save_fragment";
        public const string SaveFragmentTip = "main.save_fragment.tip";
        public const string FragmentSave = "main.fragment.save";
        public const string FragmentCancel = "main.fragment.cancel";
        public const string FragmentHint = "main.fragment.hint";
        public const string FragmentInfo = "main.fragment.info";
        public const string Original = "main.original";
        public const string OriginalTip = "main.original.tip";
        public const string Fit = "main.fit";
        public const string ViewHint = "main.view_hint";
        public const string LanguageTip = "main.language.tip";
        public const string Empty = "main.empty";
        public const string CacheEmpty = "main.cache.empty";
        public const string CacheClear = "main.cache.clear";
        public const string CacheTip = "main.cache.tip";
        public const string CacheConfirm = "main.cache.confirm";
        public const string CacheConfirmTitle = "main.cache.confirm_title";
        public const string CacheCleared = "main.cache.cleared";
        public const string CacheInUse = "main.cache.in_use";
    }

    public static class Size
    {
        public const string Kb = "size.kb";
        public const string Mb = "size.mb";
        public const string Gb = "size.gb";
    }

    public static class Dialog
    {
        public const string ChooseTarget = "dialog.choose_target";
        public const string Images = "dialog.images";
        public const string AllFiles = "dialog.all_files";
        public const string Folders = "dialog.folders";
        public const string Save = "dialog.save";
        public const string SaveFragment = "dialog.save_fragment";
        public const string ImageFilter = "dialog.image_filter";
    }

    public static class Tiles
    {
        public const string Add = "tiles.add";
        public const string NotFound = "tiles.not_found";
        public const string Counting = "tiles.counting";
        public const string None = "tiles.none";
        public const string Found = "tiles.found";
        public const string FoundFolders = "tiles.found_folders";
        public const string Few = "tiles.few";
        public const string Usable = "tiles.usable";
    }

    public static class Status
    {
        public const string CannotRead = "status.cannot_read";
        public const string Cancelling = "status.cancelling";
        public const string Cancelled = "status.cancelled";
        public const string Failed = "status.failed";
        public const string Saving = "status.saving";
        public const string Saved = "status.saved";
        public const string NotSaved = "status.not_saved";
        public const string RenderingFragment = "status.rendering_fragment";
    }

    public static class Saved
    {
        public const string Open = "saved.open";
        public const string Folder = "saved.folder";
    }

    public static class Suggest
    {
        public const string Title = "suggest.title";
        public const string Intro = "suggest.intro";
        public const string Use = "suggest.use";
        public const string Weights = "suggest.weights";
        public const string Reset = "suggest.reset";
        public const string Ratios = "suggest.ratios";
        public const string Score = "suggest.score";
        public const string ScoreHelp = "suggest.score.help";
        public const string Marks = "suggest.marks";
        public const string StatusNone = "suggest.status.none";
        public const string StatusBest = "suggest.status.best";
        public const string StatusPick = "suggest.status.pick";
        public const string DetailOff = "suggest.detail.off";
        public const string DetailSimpler = "suggest.detail.simpler";
        public const string DetailRicher = "suggest.detail.richer";
        public const string StatusAllZero = "suggest.status.all_zero";
    }

    public static class Weight
    {
        public const string ColorMatch = "weight.color_match";
        public const string ColorMatchHelp = "weight.color_match.help";
        public const string ColorVariety = "weight.color_variety";
        public const string ColorVarietyHelp = "weight.color_variety.help";
        public const string Contrast = "weight.contrast";
        public const string ContrastHelp = "weight.contrast.help";
        public const string Detail = "weight.detail";
        public const string DetailHelp = "weight.detail.help";
    }

    public static class Note
    {
        public const string ColorsWellCovered = "note.colors_well_covered";
        public const string FewPhotosInColors = "note.few_photos_in_colors";
        public const string ClearShapes = "note.clear_shapes";
        public const string LowContrast = "note.low_contrast";
        public const string Colorful = "note.colorful";
        public const string MostlyOneColor = "note.mostly_one_color";
        public const string LotsOfDetail = "note.lots_of_detail";
        public const string LittleDetail = "note.little_detail";
    }
}
