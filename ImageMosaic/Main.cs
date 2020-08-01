using ImageMosaic.Data;
using ImageMosaic.Processing;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMosaic.Helpers;

namespace ImageMosaic
{
    public partial class Main : Form
    {
        private CancellationTokenSource processCts;
        private bool isRunning;
        private readonly InputData inputData;
        private readonly ProcessingService processingService;
        private readonly Logger logger;

        public Main()
        {
            InitializeComponent();
            logger = new Logger(logBox);
            inputData = InitializeInputData();
            InitializeCancellationToken();
            processingService = new ProcessingService(outputImageBox, logger);
        }

        private void InitializeCancellationToken()
        {
            processCts = new CancellationTokenSource();
        }

        private InputData InitializeInputData()
        {
            return new InputData
            {
                PathToImagesRootFolder = pathToImagesFolder_input.Text,
                PathToOriginalImage = pathToOriginalImage_input.Text,
                PixelCount = int.Parse(pixelCount_input.Text),
                ImageWidth = int.Parse(imageWidth_input.Text),
                ImageHeight = int.Parse(imageHeight_input.Text)
            };
        }

        private void PathToImagesFolder_button_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(pathToImagesFolder_input.Text))
            {
                imageFolderBrowserDialog.SelectedPath = pathToImagesFolder_input.Text;
            }
            var openFolderResult = imageFolderBrowserDialog.ShowDialog();
            if (openFolderResult == DialogResult.OK)
            {
                inputData.PathToImagesRootFolder = imageFolderBrowserDialog.SelectedPath;
                pathToImagesFolder_input.Text = inputData.PathToImagesRootFolder;
            }
        }

        private void PathToOriginalImage_button_Click(object sender, EventArgs e)
        {
            var currentValue = pathToOriginalImage_input.Text;
            if (!string.IsNullOrEmpty(currentValue))
            {
                originalImageBrowserDialog.FileName = currentValue;
            }
            var selectFileResult = originalImageBrowserDialog.ShowDialog();
            if (selectFileResult == DialogResult.OK)
            {
                inputData.PathToOriginalImage = originalImageBrowserDialog.FileName;
                pathToOriginalImage_input.Text = inputData.PathToOriginalImage;
            }
        }
        
        private void PathToImagesFolder_input_Leave(object sender, EventArgs e)
        {
            var value = pathToImagesFolder_input.Text;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            if (Directory.Exists(value))
            {
                inputData.PathToImagesRootFolder = value;
            }
            else
            {
                MessageBox.Show("Wrong path to input folder!");
            }
        }

        private void PathToOriginalImage_input_Leave(object sender, EventArgs e)
        {
            var value = pathToOriginalImage_input.Text;
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            if (File.Exists(value))
            {
                inputData.PathToOriginalImage = value;
            }
            else
            {
                MessageBox.Show("Wrong path to original image!");
            }
        }

        private async void process_button_Click(object sender, EventArgs e)
        {
            var ct = processCts.Token;
            if (isRunning)
            {
                processCts.Cancel();
                return;
            }
            process_button.Text = "STOP";
            await Task.Factory.StartNew(() => ProcessAsync(ct).ContinueWith(AfterExecution), ct);
        }

        private async Task ProcessAsync(CancellationToken ct)
        {
            isRunning = true;
            try
            {
                await processingService.ProcessAsync(inputData, ct);
            }
            catch (Exception e)
            {
                logger.Log(e.ToString());
            }
        }

        private void AfterExecution(Task task)
        {
            isRunning = false;
            processCts = new CancellationTokenSource();
            process_button.Text = "GO";
            process_button.Refresh();
        }

        private void ImageWidth_input_Leave(object sender, EventArgs e)
        {
            int.TryParse(imageWidth_input.Text, out var value);
            if (value != 0)
            {
                inputData.ImageWidth = value;
            }
        }

        private void ImageHeight_input_Leave(object sender, EventArgs e)
        {
            int.TryParse(imageHeight_input.Text, out var value);
            if (value != 0)
            {
                inputData.ImageHeight = value;
            }
        }

        private void PixelCount_input_Leave(object sender, EventArgs e)
        {
            int.TryParse(pixelCount_input.Text, out var value);
            if (value != 0)
            {
                inputData.PixelCount = value;
            }
        }
    }
}
