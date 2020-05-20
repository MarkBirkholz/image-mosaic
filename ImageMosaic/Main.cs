﻿using ImageMosaic.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageMosaic
{
    public partial class Main : Form
    {
        private InputData inputData;
        private readonly ProcessingService processingService;
        public Main()
        {
            inputData = new InputData();
            processingService = new ProcessingService();
            InitializeComponent();
        }

        private void PathToImagesFolder_button_Click(object sender, EventArgs e)
        {
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
            processingService.Process(inputData);
        }
    }
}