namespace ImageMosaic.Data
{
    public struct Resolution
    {
        public Resolution(int w, int h)
        {
            Width = w;
            Height = h;
        }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}