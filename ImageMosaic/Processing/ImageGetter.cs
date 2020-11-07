using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
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

        public static List<Bitmap> GetImages(string path, int cellSize, CancellationToken ct)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png"));
            var result = new List<Bitmap>();
            foreach (var file in files)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
                var image = new Bitmap(file);
                //var imageWidth = image.Width;
                //var imageHeight = image.Height;
                //var ratio = 1f * imageHeight / imageWidth;
                //if (ratio > 1)
                //{
                //    imageHeight = cellSize;
                //    imageWidth = (int) Math.Round(imageHeight / ratio);
                //}
                //else
                //{
                //    imageWidth = cellSize;
                //    imageHeight = (int) Math.Round(imageWidth * ratio);
                //}
                
                result.Add(ResizeBitmap(image, cellSize, cellSize));
                image.Dispose();
            }

            return result;
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

        public static Color GetAveragePixel(IReadOnlyCollection<Color> pixels)
        {
            var pixelCount = pixels.Count;
            var r = pixels.Sum(p => p.R) / pixelCount;
            var g = pixels.Sum(p => p.G) / pixelCount;
            var b = pixels.Sum(p => p.B) / pixelCount;

            return Color.FromArgb(r, g, b);
        }
    }
}