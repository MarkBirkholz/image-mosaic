using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System;
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
        private readonly ImageGetter imageGetter;
        private readonly Logger logger;
        private readonly Validator validator;

        public ProcessingService(PictureBox outputImageBox, Logger logger)
        {
            this.outputImageBox = outputImageBox;
            this.logger = logger;
            imageGetter = new ImageGetter(logger);
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

            var drawOriginalImageAsync = DrawOriginalImageAsync(inputData, ct);
            //var buildMosaicTask = BuildMosaicAsync(inputData, ct);
            await Task.WhenAll(drawOriginalImageAsync);
        }

        private Task DrawOriginalImageAsync(InputData inputData, CancellationToken ct)
        {
            var image = imageGetter.GetImage(inputData.PathToOriginalImage, inputData.ImageWidth, inputData.ImageHeight);
            DrawImage(image);
            return PixelizeImage(image, inputData, ct);
        }

        private async Task PixelizeImage(Bitmap originalImage, InputData inputData, CancellationToken ct)
        {
            var image = originalImage.Copy();
            var pixelSizeW = image.Width / inputData.PixelCount;
            var pixelSizeH = image.Height / inputData.PixelCount;
            var tasks = new List<Task>();
            for (var i = 0; i < image.Width; i += pixelSizeW)
            {
                for (var j = 0; j < image.Height; j += pixelSizeH)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    logger.Log($"Processing pixel at {i}:{j}");
                    tasks.Add(SetBigPixel(image, pixelSizeW, pixelSizeH, i, j));
                }
            }

            await Task.WhenAll(tasks);
        }

        private Task SetBigPixel(Bitmap image, int pixelSizeW, int pixelSizeH, int i, int j)
        {
            var pixels = new List<Color>();
            for (var k = 0; k < pixelSizeW; k++)
            {
                for (var l = 0; l < pixelSizeH; l++)
                {
                    var pixel = image.GetPixel(i + k, j + l);
                    pixels.Add(pixel);
                }
            }

            var cells = pixels.Select(p => new CellData
            {
                Color = p
            }).ToList();
            var part = GenerateImage(cells);
            var pixelColor = imageGetter.GetImageColor(part);
            for (var k = 0; k < pixelSizeW; k++)
            {
                for (var l = 0; l < pixelSizeH; l++)
                {
                    image.SetPixel(i + k, j + l, pixelColor);
                }
            }
            
            DrawImage(image);

            return Task.CompletedTask;
        }

        private async Task BuildMosaicAsync(InputData inputData, CancellationToken ct)
        {
            var cells = await imageGetter.GetCellsAsync(inputData, ct);
            if (ct.IsCancellationRequested)
            {
                logger.Log("Process canceled" + Environment.NewLine);
                return;
            }

            var outputImage = GenerateImage(cells);
            var resizedImage = imageGetter.ResizeBitmap(outputImage, 300, 300);
            DrawImage(resizedImage);
            logger.Log("Done" + Environment.NewLine);
        }

        private void DrawImage(Bitmap image)
        {
            var imageCopy = image.Copy();
            outputImageBox.BeginInvoke((MethodInvoker)(() =>
            {
                outputImageBox.Width = imageCopy.Width;
                outputImageBox.Height = imageCopy.Height;
                outputImageBox.Image = imageCopy;
            }));
        }

        private Bitmap GenerateImage(IReadOnlyList<CellData> cells)
        {
            var cellCount = cells.Count;
            var imageSize = (int)Math.Ceiling(Math.Sqrt(cellCount));

            var outputImage = new Bitmap(imageSize, imageSize);
            for (var i = 0; i < imageSize; i++)
            {
                for (var j = 0; j < imageSize; j++)
                {
                    var pixelPosition = i * imageSize + j;
                    var pixel = pixelPosition < cells.Count 
                        ? cells[pixelPosition].Color 
                        : Color.White;
                    outputImage.SetPixel(i, j, pixel);
                }
            }

            return outputImage;
        }
    }

}
