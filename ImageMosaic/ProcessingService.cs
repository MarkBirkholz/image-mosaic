using ImageMosaic.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageMosaic
{
    public class ProcessingService
    {
        public void Process(InputData inputData, PictureBox outputImageBox)
        {
            var validationResult = ValidateData(inputData);
            if (!validationResult)
            {
                return;
            }

            var images = GetImages(inputData.PathToRootFolder);
            var imageDataList = ProcessImages(images);

            var outputImage = GenerateOutputImage(imageDataList);
            var resizedImage = ResizeBitmap(outputImage, 300, 300);
            outputImageBox.Width = resizedImage.Width;
            outputImageBox.Height = resizedImage.Height;
            outputImageBox.Image = resizedImage;
        }

        public Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        private Bitmap GenerateOutputImage(ImageData[] imageDataArray)
        {
            var pixelCount = imageDataArray.Length;
            var imageSize = (int)Math.Ceiling(Math.Sqrt(pixelCount));

            var outputImage = new Bitmap(imageSize, imageSize);
            for (int i = 0; i < imageSize; i++)
            {
                for (int j = 0; j < imageSize; j++)
                {
                    var pixelPosition = i * imageSize + j;
                    var pixel = pixelPosition >= imageDataArray.Length ? Color.White : imageDataArray[pixelPosition].MainColor;
                    outputImage.SetPixel(i, j, pixel);
                }
            }

            return outputImage;
        }

        private ImageData[] ProcessImages(List<Bitmap> images)
        {
            var result = new List<ImageData>();

            foreach (var image in images)
            {
                var pixel = image.GetPixel(0, 0);
                result.Add(new ImageData { MainColor = pixel });
            }

            return result.ToArray();
        }

        private List<Bitmap> GetImages(string pathToRootFolder)
        {
            var paths = Directory.GetFiles(pathToRootFolder, "*.jpg", SearchOption.AllDirectories);
            var files = paths.Select(GetFirstPixelImage);
            MessageBox.Show($"Found {files.Count()} files");

            return files.ToList();
        }

        private Bitmap GetFirstPixelImage(string path)
        {
            var image = new Bitmap(path);
            var color = image.GetPixel(0, 0);
            image.Dispose();

            var result = new Bitmap(1, 1);
            result.SetPixel(0, 0, color);
            return result;
        }

        private bool ValidateData(InputData inputData)
        {
            var errorMessages = new List<string>();
            if (string.IsNullOrEmpty(inputData.PathToRootFolder))
            {
                errorMessages.Add("Empty input folder path");
            }
            if (!Directory.Exists(inputData.PathToRootFolder))
            {
                errorMessages.Add("Directory by selected path does not exist");
            }

            if (errorMessages.Any())
            {
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
