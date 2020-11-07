using System.Drawing;

namespace ImageMosaic.Data
{
    public struct Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Resolution Resolution { get; set; }
        public Color Color { get; set; }
    }
}