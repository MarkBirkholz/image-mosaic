using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageMosaic.Processing
{
    public class ImageGetter
    {
        private readonly Logger logger;

        public ImageGetter(Logger logger)
        {
            this.logger = logger;
        }

        public async Task<List<CellData>> GetCellsAsync(InputData inputData, CancellationToken ct)
        {
            var paths = Directory.GetFiles(inputData.PathToImagesRootFolder, "*.jpg", SearchOption.AllDirectories);
            logger.Log($"Found {paths.Length} files");
            var cellTasks = paths.WithCancellation(ct).Select(GetCellAsync);
            var cells = await Task.WhenAll(cellTasks);
            return cells.Where(c => c != null).ToList();
        }

        private Task<CellData> GetCellAsync(string path)
        {
            logger.Log($"Get first pixel by {path}...");
            var originalImage = new Bitmap(path);
            var color = GetImageColor(originalImage);
            originalImage.Dispose();
            var cellImage = new Bitmap(1, 1);
            cellImage.SetPixel(0, 0, color);
            var result = new CellData
            {
                Color = color,
                Image = cellImage
            };

            return Task.FromResult(result);
        }

        public Color GetImageColor(Bitmap image)
        {
            //todo calculate color
            return image.GetPixel(0, 0);
        }

        public Bitmap GetImage(string path, int width, int height)
        {
            var image = new Bitmap(path);

            return ResizeBitmap(image, width, height);
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

        public static Color GetAveragePixel(IEnumerable<Color> pixels)
        {
            return pixels.First();
        }
    }
}