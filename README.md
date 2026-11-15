# ImageMosaic

Builds photo mosaics: a target picture of your choice is assembled from hundreds or thousands of your own
photos. Each cell of the picture is replaced by a photo (a tile) with similar colors.

Matching is purely mechanical, with no computer vision or neural networks. It uses only average colors,
the CIE Lab color space and the difference between colors.

## Glossary

The same terms are used in the program, in its tooltips and below.

- **Target picture** — the picture the mosaic reproduces.
- **Library** — all photos in the tile photo folders, subfolders included.
- **Tile** — a library photo cropped to its centered square, as placed in the mosaic.
- **Cell** — one square of the target picture; each cell gets one tile.
- **Color difference (ΔE)** — how different two colors look: 0 identical, ~2 barely noticeable,
  10 clearly different, 50+ completely different.
- **Color detail** — cells and tiles are compared as an N × N grid of average colors (3 × 3 by default).
- **Trial mosaic** — a quick mosaic 40 tiles along the long side, built for each photo when suggesting a target picture.
- **Detail** — color difference ΔE between the parts of a cell and the cell's average color.
- **Contrast** — how much lightness differs from cell to cell.
- **Color variety** — how evenly the cells spread over 12 hues plus gray.
- **Mark** — a photo's 0–100 % rating on one criterion, relative to the other photos of the library.
- **Weight** — a slider in the suggestions window: how much one point matters when choosing photos.
- **Score** — a photo's overall 0–100 rating: the weighted average of its color match, color variety and contrast
  marks.
- **Fragment** — a rectangle of whole tiles of a finished mosaic, saved on its own.

## Features

- **Several photo folders.** Add as many folders as you like with **+**; a photo reachable through two of
  them (e.g. a folder and its subfolder) is used once.
- **Suggested target pictures.** The program ranks the photos of your library by how good a mosaic each
  would make, and you pick one with a double-click; more photos appear as you scroll down. Three weight
  sliders and a detail range in the same window decide what matters (each has an **i** with a balloon), and
  moving them re-ranks the photos instantly.
- **Same detail for any orientation.** The number of tiles is set for the long side of the target picture,
  so landscape and portrait pictures of the same size get the same number of tiles.
- **Fast photo loading.** JPEGs are decoded directly at a reduced size (WIC), files are processed in
  parallel on all cores, and prepared tiles are kept in an on-disk cache between runs.
- **Accurate matching.** Cells and tiles are compared by their color detail grids, the same photo is never
  placed close to itself, and frequently used photos get a penalty.
- **Careful photo preparation.** EXIF orientation is applied, each photo is cropped to its centered square
  instead of being stretched, and identical photos are skipped.
- **Look settings applied instantly** to a finished mosaic: color correction, overlay of the target picture
  in 5 % steps, and soft **tile edges** up to 25 % in 1 % steps.
- **Saving.** **Save…** saves the whole mosaic; **Fragment…** lets you choose a part of it with a frame
  (see below). File names get the date and time, so several mosaics of one picture never overwrite each
  other, and after saving **Open** and **Show in folder** links appear.
- **Comfortable viewer.** Mouse-wheel zoom around the cursor, drag to pan, 1:1, comparison with the
  original (button or hold Space), drag & drop of a picture or folders onto the window.
- **Any number of interface languages**, one file each (see below); English and Russian are included.
- **Command-line tool** for batch processing.

## Requirements

- Windows 10/11.
- To run: [.NET Desktop Runtime 10](https://dotnet.microsoft.com/download) or newer.
- To build: .NET SDK 10 or newer.

## Getting started

```bash
dotnet run --project ImageMosaic -c Release
```

1. **Tile photo folders**: add one or more folders with your photos with **+** (or drop them onto the
   window). **−** or Delete removes the selected folders from the list. A few hundred photos already work
   well; more different photos give better results.
2. **Target picture**: choose a file, or click **Suggest a target picture from my photos…** and
   double-click one of the suggestions.
3. Set **Tiles, long side** and **Tile size**. The size of the result is shown right below.
4. **Build mosaic** (F5), the button under the matching settings. Cancel with Esc or the same button.
5. Tune the **Look** sliders below it; they change the finished mosaic immediately, without building it again.
6. **Save…** (Ctrl+S) or **Fragment…** (Ctrl+Shift+S).

Settings are remembered between runs, except the target picture.

### Saving a fragment

**Fragment…** puts a frame on the mosaic. The frame always covers whole tiles and has the proportions of
the target picture.

- Drag the frame to move it.
- Drag a corner or turn the mouse wheel to resize it; Ctrl+wheel zooms the view.
- Enter, a double-click inside the frame or **Save fragment** saves it; Esc or **Cancel** leaves.

A smaller frame gets **bigger tiles**, up to 4 times the mosaic's tile size, and the fragment is never
wider than the whole mosaic. The photos in it are read again from disk at that size, so they stay sharp
instead of being enlarged pixel by pixel. Before saving, the status line shows the fragment's size in tiles
and pixels and its tile size.

500 ms after the frame stops moving or changing size, the part inside it is shown exactly as it will be
saved, with the bigger tiles; saving then takes that picture as it is. Photos read for one frame are kept
while the frame is open, so moving it or making it smaller reads only the photos that are new to it.

### Look settings

| Slider           | 0 %                     | 50 %                        | 100 %                    |
| ---------------- | ----------------------- | --------------------------- | ------------------------ |
| Color correction | tiles keep their colors | halfway to the cell's color | exactly the cell's color |
| Overlay          | tiles only              | half picture, half tiles    | picture only             |

**Tile edges** go from 0 % (sharp) to 25 % in 1 % steps: at N % neighbouring tiles fade into each other over a
band N % of a tile wide.

### Command line

```bash
dotnet run --project ImageMosaic.Cli -c Release -- --target photo.jpg --tiles D:\Photos --tiles E:\Archive --out mosaic.jpg --long-side 120 --tile-size 32
```

Several tile folders are given by repeating `--tiles` or separating them with `;`.

| Option             | Default   | Meaning                                  |
| ------------------ | --------- | ---------------------------------------- |
| `--long-side`      | 80        | tiles along the long side                |
| `--tile-size`      | 32        | tile size, px                            |
| `--grid`           | 3         | color detail, 1–8                        |
| `--repeat`         | 4         | min distance between equal photos, cells |
| `--penalty`        | 3         | penalty for reusing a photo, ΔE          |
| `--correction`     | 0.4       | color correction, 0–1                    |
| `--overlay`        | 0.2       | overlay, 0–1                             |
| `--blend`          | 0         | tile edges, 0–0.25                       |
| `--seed`           | 1         | fill order; same seed, same mosaic       |
| `--quality`        | 92        | JPEG quality                             |
| `--cache <folder>` | see below | tile cache folder                        |
| `--no-cache`       |           | do not use the tile cache                |
| `--lang <code>`    | system    | language of messages                     |

## How it works

1. **Scanning.** Every folder is walked recursively; a file found through several folders is used once.
   Extensions are matched case-insensitively; hidden, system and inaccessible folders are skipped.
2. **Tile preparation.** Each photo is decoded at "tile size × 2" on its shorter side (for JPEG this is
   fast scaling during decoding), the EXIF rotation is applied, the centered square is cropped and scaled
   down to the tile size with an exact area average. Unreadable files and pixel-identical duplicates are
   skipped.
3. **Cache.** Prepared tiles are stored in one file per "folder + tile size". An entry stays valid while
   the file keeps the same size and modification time.
4. **Cells.** The target picture is split into square cells: the long side gets the set number of them, the
   short side as many as its proportions give. The color detail grid of each cell and each tile is computed
   in Lab.
5. **Matching.** Cells are filled in a random but reproducible order. For each cell the tiles closest in
   average color are compared in detail; the one with the smallest color difference over the grid, plus a
   penalty for frequently used photos, wins. Photos already placed within the repeat distance are excluded.
6. **Rendering.** Tiles are drawn in parallel by rows. With tile edges above 0, each pixel near a seam mixes
   the neighbouring tiles with weights that change linearly across the band and always add up to 1 (a tile
   continues past its edge as a mirror image of itself). Then color correction and overlay are applied.

### How target pictures are suggested

For each photo of the library (up to 1,500, sampled evenly from larger libraries) the program builds a trial
mosaic and measures it. Three weights decide the order:

| Slider        | Right                                    | Left                              |
| ------------- | ---------------------------------------- | --------------------------------- |
| Color match   | colors your library has plenty of        | colors hardly matter              |
| Color variety | colorful pictures: sea, sunset, city     | single-tone pictures are fine too |
| Contrast      | clear light and dark shapes, silhouettes | soft, misty pictures are fine too |

They go from 0 (does not matter) to 100; defaults are color match 40, color variety 25, contrast 20. With
all three at 0, they count equally. Each point is rated relative to your own photos (the weakest 2 % get
0 %, the strongest 2 % get 100 %), so every slider changes the order whatever your photos are like.

**Detail** is a range with two handles instead of a weight. The photos are sorted from the simplest
(sky, water, large shapes) to the most detailed (streets, crowds, leaves); the range keeps only the photos
between its handles, so 10–90 % leaves out a tenth of the simplest and a tenth of the most detailed ones.
It does not change the score of the photos it keeps.

The best 24 photos are shown first; the next ones appear as you scroll down.

The analysis is kept while the program runs: opening the window again is instant unless photos were added,
removed or edited, or the tile size, color detail or matching settings changed.

### Accuracy of matching

With repeats within 0 and variety 0 every cell gets the photo closest in color. A larger repeat distance or
more variety gives fewer visible repeats and uses more of your photos, but a cell more often gets a photo
that is not the best by color. After building, the status line shows the mean color difference ΔE (lower
is more exact), so settings can be compared on your own photos.

## Interface languages

Texts live in the `Languages` folder, one JSON file per language; the file name is the language code:

```json
{
  "name": "Русский",
  "strings": { "main.build": "Собрать мозаику", "…": "…" }
}
```

Every file there appears in the language list on the toolbar, so a new language is a new file: copy
`en.json`, translate the texts, set `name`. Missing texts are shown in English. In the code, keys are
constants of the `Texts` class (`T(Texts.Main.Build)`); a test checks that `Texts`, `en.json` and every
other language file have the same keys.

## Project structure

| Folder              | Contents                               |
| ------------------- | -------------------------------------- |
| `ImageMosaic.Core`  | all logic, no UI                       |
| `ImageMosaic`       | WinForms app                           |
| `ImageMosaic.Cli`   | command-line tool `mosaic.exe`         |
| `ImageMosaic.Tests` | xUnit tests                            |
| `Languages`         | interface texts, one file per language |

Main classes of the core: `Imaging/PixelBuffer` and `Imaging/ImageCodec` (images in memory and on disk),
`Tiles/TileLibrary` and `Tiles/TileCache` (tiles and their cache), `Mosaic/TargetGrid`,
`Mosaic/TileMatcher`, `Mosaic/MosaicRenderer` (the build steps), `Suggestions/TargetSuggester` and
`Suggestions/ScoreScale` (suggestions), `Localization/Lang` (texts), `MosaicBuilder` (the whole pipeline,
re-rendering and fragments).

## Tests

```bash
dotnet test
```

Covered: color conversion, resizing and cropping, EXIF orientations, reading and writing files, folder
scanning, several folders, the tile cache (damaged, changed files, clearing), deduplication, matching,
rendering (including tile edges), cell counts for landscape and portrait pictures, fragments (same and
bigger tiles, photos read once while the frame moves), suggestions (scores, weights, the detail range,
marks relative to the library, photos of every folder, reusing an analysis), interface texts (every key
used in the code exists, every language has the same keys and placeholders, no unused texts, adding a
language by a file), settings validation and saving, and building a whole mosaic end to end.
The tests always run in English.

## Where data is stored

- Settings: `%APPDATA%\ImageMosaic\settings.json`.
- Tile cache: `%LOCALAPPDATA%\ImageMosaic\TileCache`, one file per folder and tile size. It is kept when the
  program closes; **Clear tile cache** at the bottom of the sidebar deletes it.

For testing, the environment variables `IMAGEMOSAIC_SETTINGS` (settings file) and `IMAGEMOSAIC_CACHE_DIR`
(tile cache folder) point the program at other locations, so test runs never touch your own settings and
cache.

## License

Copyright (C) 2026 Mark Birkholz

ImageMosaic is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
option) any later version. It is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
see the [LICENSE](LICENSE) file for details.
