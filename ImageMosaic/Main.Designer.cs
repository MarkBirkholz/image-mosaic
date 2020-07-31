namespace ImageMosaic
{
    partial class Main
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pathToImagesFolder_label = new System.Windows.Forms.Label();
            this.pathToImagesFolder_input = new System.Windows.Forms.TextBox();
            this.pathToImagesFolder_button = new System.Windows.Forms.Button();
            this.imageFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.process_button = new System.Windows.Forms.Button();
            this.outputImageBox = new System.Windows.Forms.PictureBox();
            this.logBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pathToImagesFolder_label
            // 
            this.pathToImagesFolder_label.AutoSize = true;
            this.pathToImagesFolder_label.Location = new System.Drawing.Point(13, 9);
            this.pathToImagesFolder_label.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.pathToImagesFolder_label.Name = "pathToImagesFolder_label";
            this.pathToImagesFolder_label.Size = new System.Drawing.Size(146, 17);
            this.pathToImagesFolder_label.TabIndex = 0;
            this.pathToImagesFolder_label.Text = "Path to images folder:";
            // 
            // pathToImagesFolder_input
            // 
            this.pathToImagesFolder_input.Location = new System.Drawing.Point(13, 30);
            this.pathToImagesFolder_input.Margin = new System.Windows.Forms.Padding(4);
            this.pathToImagesFolder_input.Name = "pathToImagesFolder_input";
            this.pathToImagesFolder_input.Size = new System.Drawing.Size(229, 22);
            this.pathToImagesFolder_input.TabIndex = 1;
            this.pathToImagesFolder_input.Text = "D:\\Mark\\Files\\img";
            this.pathToImagesFolder_input.Leave += new System.EventHandler(this.PathToImagesFolder_input_Leave);
            // 
            // pathToImagesFolder_button
            // 
            this.pathToImagesFolder_button.Location = new System.Drawing.Point(253, 30);
            this.pathToImagesFolder_button.Margin = new System.Windows.Forms.Padding(4);
            this.pathToImagesFolder_button.Name = "pathToImagesFolder_button";
            this.pathToImagesFolder_button.Size = new System.Drawing.Size(133, 28);
            this.pathToImagesFolder_button.TabIndex = 8;
            this.pathToImagesFolder_button.Text = "Select...";
            this.pathToImagesFolder_button.UseVisualStyleBackColor = true;
            this.pathToImagesFolder_button.Click += new System.EventHandler(this.PathToImagesFolder_button_Click);
            // 
            // imageFolderBrowserDialog
            // 
            this.imageFolderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // process_button
            // 
            this.process_button.Location = new System.Drawing.Point(17, 296);
            this.process_button.Margin = new System.Windows.Forms.Padding(4);
            this.process_button.Name = "process_button";
            this.process_button.Size = new System.Drawing.Size(179, 54);
            this.process_button.TabIndex = 3;
            this.process_button.Text = "GO";
            this.process_button.UseVisualStyleBackColor = true;
            this.process_button.Click += new System.EventHandler(this.process_button_Click);
            // 
            // outputImageBox
            // 
            this.outputImageBox.Location = new System.Drawing.Point(220, 79);
            this.outputImageBox.Margin = new System.Windows.Forms.Padding(4);
            this.outputImageBox.Name = "outputImageBox";
            this.outputImageBox.Size = new System.Drawing.Size(133, 62);
            this.outputImageBox.TabIndex = 4;
            this.outputImageBox.TabStop = false;
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(13, 358);
            this.logBox.Margin = new System.Windows.Forms.Padding(4);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(678, 183);
            this.logBox.TabIndex = 0;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 554);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.pathToImagesFolder_label);
            this.Controls.Add(this.process_button);
            this.Controls.Add(this.outputImageBox);
            this.Controls.Add(this.pathToImagesFolder_button);
            this.Controls.Add(this.pathToImagesFolder_input);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Main";
            this.Text = "Image mosaic";
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.FolderBrowserDialog imageFolderBrowserDialog;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.PictureBox outputImageBox;
        private System.Windows.Forms.Button pathToImagesFolder_button;
        private System.Windows.Forms.TextBox pathToImagesFolder_input;
        private System.Windows.Forms.Label pathToImagesFolder_label;
        private System.Windows.Forms.Button process_button;

        #endregion
    }
}

