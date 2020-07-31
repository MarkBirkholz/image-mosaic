using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageMosaic.Processing
{
    public class ImageReader
    {
        private readonly Logger logger;

        public ImageReader(Logger logger)
        {
            this.logger = logger;
        }

        public async Task<List<Bitmap>> GetImagesAsync(InputData inputData)
        {
            var paths = Directory.GetFiles(inputData.PathToRootFolder, "*.jpg", SearchOption.AllDirectories);
            logger.Log($"Found {paths.Length} files");
            var filesTask = paths.Select(GetFirstPixelImageAsync);
            var files = await Task.WhenAll(filesTask);
            
            return files.ToList();
        }

        private async Task<Bitmap> GetFirstPixelImageAsync(string path)
        {
            logger.Log($"Get first pixel by {path}...");
            var image = new Bitmap(path);
            var color = image.GetPixel(0, 0);
            image.Dispose();

            var result = new Bitmap(1, 1);
            result.SetPixel(0, 0, color);

            return result;
        }

    }
}