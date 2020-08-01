using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImageMosaic.Data;
using ImageMosaic.Helpers;

namespace ImageMosaic.Processing
{
    public class Validator
    {
        private readonly Logger logger;

        public Validator(Logger logger)
        {
            this.logger = logger;
        }

        public bool ValidateData(InputData inputData)
        {
            var errorMessages = new List<string>();
            if (string.IsNullOrEmpty(inputData.PathToImagesRootFolder))
            {
                errorMessages.Add("Empty input folder path");
            }
            if (!Directory.Exists(inputData.PathToImagesRootFolder))
            {
                errorMessages.Add("Directory by selected path does not exist");
            }

            if (!File.Exists(inputData.PathToOriginalImage))
            {
                errorMessages.Add("Original image doesn't exist");
            }

            if (errorMessages.Any())
            {
                logger.Log(errorMessages);
                MessageBox.Show(string.Join(Environment.NewLine, errorMessages));
                return false;
            }

            return true;
        }
    }
}