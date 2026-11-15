# ImageMosaic

Builds photo mosaics: a picture of your choice is assembled from hundreds or thousands of your own photos.
Each cell of the picture is replaced by a photo with a similar color.

Matching is purely mechanical, with no computer vision or neural networks. It uses only average colors,
the CIE Lab color space and the distance between colors.

## Features

- **Fast photo loading.** JPEGs are decoded directly at a reduced size (WIC), and files are processed
  in parallel on all cores.
- **On-disk tile cache.** A second run over the same folder decodes no photos at all; changed and new
  files are picked up automatically.
- **Accurate matching with a color grid.** A cell and a photo are compared not by a single average color
  but by an N×N grid of averages (3×3 by default). At the border between sky and land, the mosaic uses
  photos that are light at the top and dark at the bottom.
- **No repeats nearby.** The same photo is never placed closer than a set number of cells to itself, and
  frequently used photos get a penalty, so more different photos end up in the mosaic.
- **Careful photo preparation.** EXIF orientation is applied (phone shots are not lying on their side),
  each photo is cropped to its centered square instead of being stretched, and identical photos are skipped.
- **Color correction and overlay.** Adjusted with sliders and applied to a finished mosaic instantly,
  without matching again.
- **Comfortable viewer.** Mouse-wheel zoom around the cursor, drag to pan, 1:1, comparison with the
  original (button or hold Space), drag & drop of a file or folder onto the window.
- **Command-line tool** for batch processing.

## Requirements

- Windows 10/11.
- To run: [.NET Desktop Runtime 10](https://dotnet.microsoft.com/download) or newer.
  The app also runs on newer .NET versions.
- To build: .NET SDK 10 or newer.

## Getting started

```bash
dotnet run --project ImageMosaic -c Release
```

1. **Target image**: the picture to reproduce.
2. **Tile photos**: a folder with your photos; subfolders are included. The more different photos, the
   better the result; a few hundred already work well.
3. Set the number of tiles across and the tile size in pixels. The resulting image size is shown right
   below the fields.
4. **Build mosaic** (F5). Cancel with Esc or the same button.
5. Tune **Color correction** and **Overlay**; the image updates immediately.
6. **Save as…** (Ctrl+S): JPEG, PNG or TIFF.

Settings are remembered between runs.

### Command line

```bash
dotnet run --project ImageMosaic.Cli -c Release -- --target photo.jpg --tiles D:\Photos --out mosaic.jpg --columns 120 --tile-size 32
```

| Option | Default | What it does |
|---|---|---|
| `--columns` | 80 | tiles across; the number of rows follows from the picture's aspect ratio |
| `--tile-size` | 32 | size of one tile in the output image, px |
| `--grid` | 3 | colors compared per tile side (1–8) |
| `--repeat` | 4 | minimum distance between identical photos, in cells |
| `--penalty` | 3 | penalty for using the same photo often (in ΔE units) |
| `--correction` | 0.4 | shift of a photo's color toward its cell's color (0–1) |
| `--overlay` | 0.2 | opacity of the target picture laid over the mosaic (0–1) |
| `--seed` | 1 | seed of the fill order; the same inputs and seed give the same mosaic |
| `--quality` | 92 | JPEG quality |
| `--cache <folder>` / `--no-cache` | `%LOCALAPPDATA%\ImageMosaic\TileCache` | where to keep the tile cache |

## How it works

1. **Scanning.** The folder is walked recursively. Extensions are matched case-insensitively (`.JPG`
   works too); hidden, system and inaccessible folders are skipped.
2. **Tile preparation.** Each photo is decoded at "tile size × 2" on its shorter side (for JPEG this is
   fast scaling during decoding). Then the EXIF rotation is applied, the centered square is cropped and
   scaled down to the tile size with an exact area average. Unreadable files are logged and skipped, and
   pixel-identical duplicates are dropped.
3. **Cache.** Prepared tiles are stored in one file per "folder + tile size" pair. An entry stays valid
   while the file keeps the same size and modification time.
4. **Target.** The picture is split into `columns × rows` cells. For each cell an N×N grid of average
   colors in Lab is computed, and the same is done for each tile.
5. **Matching.** Cells are filled in a random but reproducible order. For each cell, the candidates
   closest in average color are taken; candidate lists are shared between cells of similar color. The
   candidate with the lowest cost wins: RMS ΔE over the grid plus a usage penalty. Photos already placed
   within the repeat radius are excluded. If there are fewer photos than cells in the repeat window, the
   radius is reduced automatically.
6. **Rendering.** Tiles are copied into the output image (in parallel by rows). Optionally, each tile's
   average color is shifted toward its cell's color and the target picture is blended on top.

### Performance

Measured on a 16-thread CPU:

| Scenario | Time |
|---|---|
| 205 photos, 1080p–4K, loaded the old way (GDI+, sequential) | 6.8 s |
| the same 205 photos, new loader (parallel, decoding at reduced size) | 0.64 s |
| the same 205 photos from the cache | 0.01 s |
| full 130×81-tile mosaic (4160×2592 px) from 2000 photos, no cache | 0.6 s |
| the same with the cache | 0.3 s |

## Project structure

| Project | Purpose |
|---|---|
| `ImageMosaic.Core` | all the logic: image loading, cache, matching, rendering; no UI dependencies |
| `ImageMosaic` | WinForms app |
| `ImageMosaic.Cli` | command-line tool `mosaic.exe` |
| `ImageMosaic.Tests` | xUnit tests |

Main classes of the core:

- `Imaging/PixelBuffer`: an in-memory 24-bit image; resize, crop, EXIF rotation;
- `Imaging/ImageCodec`: reading and writing through WIC;
- `Tiles/TileLibrary`, `Tiles/TileCache`: tile loading and the cache;
- `Mosaic/TargetGrid`, `Mosaic/TileMatcher`, `Mosaic/MosaicRenderer`: the build steps;
- `MosaicBuilder`: the whole pipeline, plus fast re-rendering of colors.

## Tests

```bash
dotnet test
```

Covered: color conversion, resizing and cropping, all 8 EXIF orientations, reading and writing files,
folder scanning, the cache (including a damaged one and changed files), deduplication, matching (exact
match, color grid, repeat radius, penalty, determinism), rendering, settings validation and building a
whole mosaic end to end.

## Where data is stored

- Settings: `%APPDATA%\ImageMosaic\settings.json`.
- Tile cache: `%LOCALAPPDATA%\ImageMosaic\TileCache`. It can be deleted at any time and will be rebuilt.
