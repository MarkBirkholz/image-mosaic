namespace ImageMosaic.Data
{
    public class InputData
    {
        public string PathToImagesRootFolder { get; set; }
        public string PathToOriginalImage { get; set; }
        public int CellSize { get; set; }
        public int ImageBoxWidth { get; set; }
        public int ImageBoxHeight { get; set; }
        public int ImagePadding { get; set; }
    }
}
