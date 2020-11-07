using System;
using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaic.Processing
{
    public class ProcessingService
    {
        private readonly PictureBox outputImageBox;
        private readonly Logger logger;
        private readonly Validator validator;

        public ProcessingService(PictureBox outputImageBox, Logger logger)
        {
            this.outputImageBox = outputImageBox;
            this.logger = logger;
            validator = new Validator(logger);
        }

        public async Task ProcessAsync(InputData inputData, CancellationToken ct)
        {
            logger.Log($"Start processing {inputData.PathToImagesRootFolder} folder...");
            var validationResult = validator.ValidateData(inputData);
            if (!validationResult)
            {
                return;
            }

            await DrawOriginalImageAsync(inputData, ct);
        }

        private async Task DrawOriginalImageAsync(InputData inputData, CancellationToken ct)
        {
            var sourceImagesTask = LoadSourceImages(inputData.PathToImagesRootFolder, inputData.CellSize, ct);
            var image = ImageGetter.GetImage(inputData.PathToOriginalImage, inputData);
            DrawImage(inputData, image, image.Width, image.Height);
            var cells = PixelizeImage(image, inputData, ct);
            var cellImages = await sourceImagesTask;
            logger.Log($"Processing {cellImages.Count} source images");
            BuildMosaic(image, cells, cellImages);
            DrawImage(inputData, image, image.Width, image.Height);
        }

        private static async Task<List<CellData>> LoadSourceImages(
            string pathToImagesRootFolder,
            int cellSize,
            CancellationToken ct)
        {
            var images = ImageGetter.GetImages(pathToImagesRootFolder, cellSize, ct);

            var cells = images
                .TakeWhile(image => !ct.IsCancellationRequested)
                .Select(image => new CellData
                {
                    Image = image,
                    Color = ImageGetter.GetAveragePixel(GetImagePixels(image, 0, 0, image.Width, image.Height))
                }).ToList();
            FillCellBackground(cells);

            return cells;
        }

        private static void FillCellBackground(IEnumerable<CellData> cells)
        {
            foreach (var cellData in cells)
            {
                
            }
        }

        private List<Cell> PixelizeImage(Bitmap image, InputData inputData, CancellationToken ct)
        {
            var cells = new List<Cell>();
            logger.Log("Begin pixelizing");
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            for (var cellPositionX = 0; cellPositionX < imageWidth; cellPositionX += inputData.CellSize)
            {
                for (var cellPositionY = 0; cellPositionY < imageHeight; cellPositionY += inputData.CellSize)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return cells;
                    }
                    var cell = SetCellColor(image, inputData.CellSize, cellPositionX, cellPositionY);
                    cells.Add(cell);
                }
            }

            DrawImage(inputData, image, imageWidth, imageHeight);
            logger.Log("End pixelizing");

            return cells;
        }

        private static void BuildMosaic(Bitmap image, IEnumerable<Cell> cells, IReadOnlyCollection<CellData> cellImages)
        {
            foreach (var cell in cells)
            {
                var cellImage = cellImages.FirstOrDefault(cellData => AreMatched(cellData.Color, cell.Color, 10));
                for (var cellPixelPositionX = 0; cellPixelPositionX < cell.Resolution.Width; cellPixelPositionX++)
                {
                    for (var cellPixelPositionY = 0; cellPixelPositionY < cell.Resolution.Height; cellPixelPositionY++)
                    {
                        var pixelColor = cellImage?.Image.GetPixel(cellPixelPositionX, cellPixelPositionY)
                                         ?? cell.Color;
                        image.SetPixel(cell.X + cellPixelPositionX, cell.Y + cellPixelPositionY, pixelColor);
                    }
                }
            }
        }

        private static bool AreMatched(Color c1, Color c2, int matchRatio)
        {
            return Math.Abs(c1.R - c2.R) <= matchRatio &&
                   Math.Abs(c1.G - c2.G) <= matchRatio &&
                   Math.Abs(c1.G - c2.G) <= matchRatio;
        }

        private static Cell SetCellColor(Bitmap image, int cellSize, int cellPositionX, int cellPositionY)
        {
            var cellSizeW = image.Width < cellPositionX + cellSize
                ? image.Width - cellPositionX
                : cellSize;
            var cellSizeH = image.Height < cellPositionY + cellSize
                ? image.Height - cellPositionY
                : cellSize;

            var pixels = GetImagePixels(image, cellPositionX, cellPositionY, cellSizeW, cellSizeH);

            var pixelColor = ImageGetter.GetAveragePixel(pixels);
            for (var pixelPositionX = 0; pixelPositionX < cellSizeW; pixelPositionX++)
            {
                for (var pixelPositionY = 0; pixelPositionY < cellSizeH; pixelPositionY++)
                {
                    image.SetPixel(cellPositionX + pixelPositionX, cellPositionY + pixelPositionY, pixelColor);
                }
            }

            return new Cell
            {
                X = cellPositionX,
                Y = cellPositionY,
                Resolution = new Resolution(cellSizeW, cellSizeH),
                Color = pixelColor
            };
        }

        private static List<Color> GetImagePixels(Bitmap image, int x, int y, int width, int height)
        {
            var pixels = new List<Color>();
            for (var pixelPositionX = 0; pixelPositionX < width; pixelPositionX++)
            {
                for (var pixelPositionY = 0; pixelPositionY < height; pixelPositionY++)
                {
                    var pixel = image.GetPixel(x + pixelPositionX, y + pixelPositionY);
                    pixels.Add(pixel);
                }
            }

            return pixels;
        }

        private void DrawImage(InputData inputData, Bitmap originalImage, int width, int height)
        {
            var paddingV = Math.Max(inputData.ImagePadding, (int) Math.Round((inputData.ImageBoxHeight - height) / 2f));
            var paddingH = Math.Max(inputData.ImagePadding, (int) Math.Round((inputData.ImageBoxWidth - width) / 2f));
            
            var image = originalImage.Copy();
            outputImageBox.BeginInvoke((MethodInvoker)(() =>
            {
                outputImageBox.Image = image;
                outputImageBox.SetBounds(paddingH, paddingV, width, height);
            }));
        }
    }

}
