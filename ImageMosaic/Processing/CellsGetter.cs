using ImageMosaic.Data;
using ImageMosaic.Helpers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageMosaic.Processing
{
    public class CellsGetter
    {
        private readonly Logger logger;

        public CellsGetter(Logger logger)
        {
            this.logger = logger;
        }

        public async Task<List<CellData>> GetCellsAsync(InputData inputData)
        {
            var paths = Directory.GetFiles(inputData.PathToRootFolder, "*.jpg", SearchOption.AllDirectories);
            logger.Log($"Found {paths.Length} files");
            var cellsTask = paths.Select(GetCellAsync);
            var cells = await Task.WhenAll(cellsTask);
            return cells.ToList();
        }

        private async Task<CellData> GetCellAsync(string path)
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

            return result;
        }

        private static Color GetImageColor(Bitmap image)
        {
            //todo calculate color
            return image.GetPixel(0, 0);
        }
    }
}