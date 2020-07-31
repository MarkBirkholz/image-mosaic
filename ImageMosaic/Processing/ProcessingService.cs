﻿using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageMosaic.Processing
{
    public class ProcessingService
    {
        private readonly PictureBox outputImageBox;
        private readonly ImageReader imageReader;
        private readonly Logger logger;

        public ProcessingService(PictureBox outputImageBox, Logger logger)
        {
            this.outputImageBox = outputImageBox;
            this.logger = logger;
            imageReader = new ImageReader(logger);
        }

        public async Task ProcessAsync(InputData inputData, PictureBox outputImageBox)
        {
            logger.Log($"Start processing {inputData.PathToRootFolder} folder...");
            var validationResult = ValidateData(inputData);
            if (!validationResult)
            {
                return;
            }

            var images = await imageReader.GetImagesAsync(inputData);
            /*
            var imageDataList = ProcessImages(images);

            var outputImage = GenerateOutputImage(imageDataList);
            var resizedImage = ResizeBitmap(outputImage, 300, 300);
            outputImageBox.Width = resizedImage.Width;
            outputImageBox.Height = resizedImage.Height;
            outputImageBox.Image = resizedImage;
            */
            logger.Log("Done" + Environment.NewLine);
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
