using ImageMosaic.Data;
using ImageMosaic.Processing;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMosaic.Helpers;

namespace ImageMosaic
{
    public partial class Main : Form
    {
        private InputData inputData;
        private readonly ProcessingService processingService;
        private readonly Logger logger;
        public Main()
        {
            InitializeComponent();
            logger = new Logger(logBox);
            inputData = new InputData
            {
                PathToRootFolder = pathToImagesFolder_input.Text
            };
            processingService = new ProcessingService(outputImageBox, logger);
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
                inputData.PathToRootFolder = imageFolderBrowserDialog.SelectedPath;
                pathToImagesFolder_input.Text = inputData.PathToRootFolder;
            }
        }

        private void PathToImagesFolder_input_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pathToImagesFolder_input.Text))
            {
                return;
            }
            if (Directory.Exists(pathToImagesFolder_input.Text))
            {
                inputData.PathToRootFolder = pathToImagesFolder_input.Text;
            }
            else
            {
                MessageBox.Show("Wrong path to input folder!");
            }
        }

        private async void process_button_Click(object sender, EventArgs e)
        {
            process_button.Text = "STOP";
            await Task.Factory.StartNew(() => ProcessAsync().ContinueWith(AfterExecution));
        }

        private async Task ProcessAsync()
        {
            try
            {
                await processingService.ProcessAsync(inputData);
            }
            catch (Exception e)
            {
                logger.Log(e.ToString());
            }
        }

        private void AfterExecution(Task obj)
        {
            process_button.Text = "GO";
            process_button.Refresh();
        }
    }
}
