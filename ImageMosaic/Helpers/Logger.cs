using System;
using System.Collections.Generic;
using System.Linq;
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
            var line = FormatMessage(message);
            WriteLog(line);
        }

        public void Log(IEnumerable<string> messages)
        {
            var lines = messages.Select(FormatMessage);
            foreach (var line in lines)
            {
                WriteLog(line);
            }
        }
        
        private static string FormatMessage(string message)
        {
            return DateTime.Now.TimeOfDay + ": " + message;
        }

        private void WriteLog(string line)
        {
            logBox.BeginInvoke((MethodInvoker)(() => logBox.AppendText(line + Environment.NewLine)));
        }

    }
}