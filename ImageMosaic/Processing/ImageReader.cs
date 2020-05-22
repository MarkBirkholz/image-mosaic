using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageMosaic.Processing
{
    public class ImageReader
    {
        private readonly Logger logger;

        public ImageReader(Logger logger)
        {
            this.logger = logger;
        }

        public List<Bitmap> GetImages(InputData inputData)
        {
            var paths = Directory.GetFiles(inputData.PathToRootFolder, "*.jpg", SearchOption.AllDirectories);
            var files = paths.Select(GetFirstPixelImage);
            logger.Log($"Found {files.Count()} files");

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

    }
}