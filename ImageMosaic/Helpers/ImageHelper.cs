using System.Drawing;

namespace ImageMosaic.Helpers
{
    public static class ImageHelper
    {
        public static Bitmap Copy(this Bitmap original)
        {
            var outputImage = new Bitmap(original.Width, original.Height);
            for (var i = 0; i < original.Width; i++)
            {
                for (var j = 0; j < original.Height; j++)
                {
                    var pixel = original.GetPixel(i, j);
                    outputImage.SetPixel(i, j, pixel);
                }
            }

            return outputImage;
        }
    }
}