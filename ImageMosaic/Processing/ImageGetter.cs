using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageMosaic.Data;

namespace ImageMosaic.Processing
{
    public static class ImageGetter
    {
        public static Bitmap GetImage(string path, InputData inputData)
        {
            var image = new Bitmap(path);

            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var ratio = 1f * imageHeight / imageWidth;
            if (ratio > 1)
            {
                imageHeight = inputData.ImageBoxHeight - 2 * inputData.ImagePadding;
                imageWidth = (int) Math.Round(imageHeight / ratio);
            }
            else
            {
                imageWidth = inputData.ImageBoxWidth - 2 * inputData.ImagePadding;
                imageHeight = (int) Math.Round(imageWidth * ratio);
            }
            return ResizeBitmap(image, imageWidth, imageHeight);
        }

        private static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            return result;
        }

        public static Color GetAveragePixel(IEnumerable<Color> pixels)
        {
            return pixels.First();
        }
    }
}