using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageMosaic.Processing
{
    public static class ImageGetter
    {
        public static Bitmap GetImage(string path, int width, int height)
        {
            var image = new Bitmap(path);

            return ResizeBitmap(image, width, height);
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