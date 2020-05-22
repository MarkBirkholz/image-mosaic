using System;
using System.Windows.Forms;

namespace ImageMosaic.Helpers
{
    public class Logger
    {
        private TextBox logBox;

        public Logger(TextBox logBox)
        {
            this.logBox = logBox;
        }

        public void Log(string message)
        {
            logBox.Text += message + Environment.NewLine;
        }
    }
}