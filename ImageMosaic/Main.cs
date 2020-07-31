using ImageMosaic.Data;
using ImageMosaic.Processing;
using System;
using System.IO;
using System.Windows.Forms;

namespace ImageMosaic
{
    /// <summary>
    /// 1. Перевести расчет в фон
    /// 2. перевести чтение файлов на асинхронное чтение пачками
    /// 3. залогировать все шаги, добавить вывод времени
    /// ...
    /// </summary>
    public partial class Main : Form
    {
        private InputData inputData;
        private readonly ProcessingService processingService;
        public Main()
        {
            inputData = new InputData();
            InitializeComponent();
            processingService = new ProcessingService(outputImageBox, logBox);
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

        private void process_button_Click(object sender, EventArgs e)
        {
            processingService.Process(inputData, outputImageBox);
        }
    }
}
