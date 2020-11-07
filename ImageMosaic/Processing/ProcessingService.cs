using System;
using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
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

        private Task DrawOriginalImageAsync(InputData inputData, CancellationToken ct)
        {
            var image = ImageGetter.GetImage(inputData.PathToOriginalImage, inputData);
            DrawImage(inputData, image, image.Width, image.Height);
            return PixelizeImage(image, inputData, ct);
        }

        private async Task PixelizeImage(Bitmap image, InputData inputData, CancellationToken ct)
        {
            logger.Log("Begin pixelizing");
            var imageWidth = image.Width;
            var imageHeight = image.Height;
            for (var cellPositionX = 0; cellPositionX < imageWidth; cellPositionX += inputData.CellSize)
            {
                for (var cellPositionY = 0; cellPositionY < imageHeight; cellPositionY += inputData.CellSize)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                    await SetCellColor(image, inputData.CellSize, cellPositionX, cellPositionY);
                }
            }

            DrawImage(inputData, image, imageWidth, imageHeight);
            logger.Log("End pixelizing");
        }

        private static Task SetCellColor(Bitmap image, int cellSize, int cellPositionX, int cellPositionY)
        {
            return Task.Factory.StartNew(() =>
            {
                var cellSizeW = image.Width < cellPositionX + cellSize
                    ? image.Width - cellPositionX
                    : cellSize;
                var cellSizeH = image.Height < cellPositionY + cellSize
                    ? image.Height - cellPositionY
                    : cellSize;

                var pixels = new List<Color>();
                for (var pixelPositionX = 0; pixelPositionX < cellSizeW; pixelPositionX++)
                {
                    for (var pixelPositionY = 0; pixelPositionY < cellSizeH; pixelPositionY++)
                    {
                        var pixel = image.GetPixel(cellPositionX + pixelPositionX, cellPositionY + pixelPositionY);
                        pixels.Add(pixel);
                    }
                }

                var pixelColor = ImageGetter.GetAveragePixel(pixels);
                for (var pixelPositionX = 0; pixelPositionX < cellSizeW; pixelPositionX++)
                {
                    for (var pixelPositionY = 0; pixelPositionY < cellSizeH; pixelPositionY++)
                    {
                        image.SetPixel(cellPositionX + pixelPositionX, cellPositionY + pixelPositionY, pixelColor);
                    }
                }
            });
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
