using ImageMosaic.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageMosaic
{
    public class ProcessingService
    {
        public void Process(InputData inputData)
        {
            var validationResult = ValidateData(inputData);
            if (!validationResult)
            {
                return;
            }
        }

        private bool ValidateData(InputData inputData)
        {
            var errorMessages = new List<string>();
            if (string.IsNullOrEmpty(inputData.PathToRootFolder))
            {
                errorMessages.Add("Empty input folder path");
            }
            if (!Directory.Exists(inputData.PathToRootFolder))
            {
                errorMessages.Add("Directory by selected path does not exist");
            }

            if (errorMessages.Any())
            {
                MessageBox.Show(string.Join(Environment.NewLine, errorMessages));
                return false;
            }

            return true;
        }
    }
}
