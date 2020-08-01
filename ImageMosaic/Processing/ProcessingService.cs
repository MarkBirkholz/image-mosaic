using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaic.Processing
{
    public class ProcessingService
    {
        private readonly PictureBox outputImageBox;
        private readonly CellsGetter cellsGetter;
        private readonly Logger logger;

        public ProcessingService(PictureBox outputImageBox, Logger logger)
        {
            this.outputImageBox = outputImageBox;
            this.logger = logger;
            cellsGetter = new CellsGetter(logger);
        }

        public async Task ProcessAsync(InputData inputData, CancellationToken ct)
        {
            logger.Log($"Start processing {inputData.PathToImagesRootFolder} folder...");
            var validationResult = ValidateData(inputData);
            if (!validationResult)
            {
                return;
            }

            var cells = await cellsGetter.GetCellsAsync(inputData, ct);
            if (ct.IsCancellationRequested)
            {
                logger.Log("Process canceled" + Environment.NewLine);
                return;
            }
            var outputImage = GenerateOutputImage(cells);
            var resizedImage = ResizeBitmap(outputImage, 300, 300);
            DrawImage(resizedImage);
            logger.Log("Done" + Environment.NewLine);
        }

        private void DrawImage(Image resizedImage)
        {
            outputImageBox.BeginInvoke((MethodInvoker)(() =>
            {
                outputImageBox.Width = resizedImage.Width;
                outputImageBox.Height = resizedImage.Height;
                outputImageBox.Image = resizedImage;
            }));
        }

        private Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        private Bitmap GenerateOutputImage(IReadOnlyList<CellData> cells)
        {
            var cellCount = cells.Count;
            var imageSize = (int)Math.Ceiling(Math.Sqrt(cellCount));

            var outputImage = new Bitmap(imageSize, imageSize);
            for (int i = 0; i < imageSize; i++)
            {
                for (int j = 0; j < imageSize; j++)
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

        private bool ValidateData(InputData inputData)
        {
            var errorMessages = new List<string>();
            if (string.IsNullOrEmpty(inputData.PathToImagesRootFolder))
            {
                errorMessages.Add("Empty input folder path");
            }
            if (!Directory.Exists(inputData.PathToImagesRootFolder))
            {
                errorMessages.Add("Directory by selected path does not exist");
            }

            if (errorMessages.Any())
            {
                logger.Log(errorMessages);
                MessageBox.Show(string.Join(Environment.NewLine, errorMessages));
                return false;
            }

            return true;
        }
        
        private class ImageData
        {
            public Color MainColor { get; set; }
        }
    }

}
