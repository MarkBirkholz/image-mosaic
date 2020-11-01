using System.Windows.Forms;

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
            this.originalImageBrowserDialog = new System.Windows.Forms.OpenFileDialog();
            this.process_button = new System.Windows.Forms.Button();
            this.outputImageBox = new System.Windows.Forms.PictureBox();
            this.logBox = new System.Windows.Forms.TextBox();
            this.pathToOriginalImage_input = new System.Windows.Forms.TextBox();
            this.pathToOriginalImage_button = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.imageWidth_input = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.imageHeight_input = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.pixelCount_input = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pathToImagesFolder_label
            // 
            this.pathToImagesFolder_label.AutoSize = true;
            this.pathToImagesFolder_label.Location = new System.Drawing.Point(10, 7);
            this.pathToImagesFolder_label.Name = "pathToImagesFolder_label";
            this.pathToImagesFolder_label.Size = new System.Drawing.Size(109, 13);
            this.pathToImagesFolder_label.TabIndex = 0;
            this.pathToImagesFolder_label.Text = "Path to images folder:";
            // 
            // pathToImagesFolder_input
            // 
            this.pathToImagesFolder_input.Location = new System.Drawing.Point(10, 24);
            this.pathToImagesFolder_input.Name = "pathToImagesFolder_input";
            this.pathToImagesFolder_input.Size = new System.Drawing.Size(173, 20);
            this.pathToImagesFolder_input.TabIndex = 1;
            this.pathToImagesFolder_input.Text = "E:\\Mark\\img\\we";
            this.pathToImagesFolder_input.Leave += new System.EventHandler(this.PathToImagesFolder_input_Leave);
            // 
            // pathToImagesFolder_button
            // 
            this.pathToImagesFolder_button.Location = new System.Drawing.Point(190, 24);
            this.pathToImagesFolder_button.Name = "pathToImagesFolder_button";
            this.pathToImagesFolder_button.Size = new System.Drawing.Size(100, 23);
            this.pathToImagesFolder_button.TabIndex = 8;
            this.pathToImagesFolder_button.Text = "Select...";
            this.pathToImagesFolder_button.UseVisualStyleBackColor = true;
            this.pathToImagesFolder_button.Click += new System.EventHandler(this.PathToImagesFolder_button_Click);
            // 
            // imageFolderBrowserDialog
            // 
            this.imageFolderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // originalImageBrowserDialog
            // 
            this.originalImageBrowserDialog.Filter = "Jpg|*.jpg|Jpeg|*.jpeg|Png|*.png";
            this.originalImageBrowserDialog.InitialDirectory = "E:\\Mark\\Files\\img";
            // 
            // process_button
            // 
            this.process_button.Location = new System.Drawing.Point(13, 240);
            this.process_button.Name = "process_button";
            this.process_button.Size = new System.Drawing.Size(134, 44);
            this.process_button.TabIndex = 3;
            this.process_button.Text = "GO";
            this.process_button.UseVisualStyleBackColor = true;
            this.process_button.Click += new System.EventHandler(this.process_button_Click);
            // 
            // outputImageBox
            // 
            this.outputImageBox.Location = new System.Drawing.Point(296, 24);
            this.outputImageBox.Name = "outputImageBox";
            this.outputImageBox.Size = new System.Drawing.Size(100, 50);
            this.outputImageBox.TabIndex = 4;
            this.outputImageBox.TabStop = false;
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(10, 291);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(510, 149);
            this.logBox.TabIndex = 0;
            // 
            // pathToOriginalImage_input
            // 
            this.pathToOriginalImage_input.Location = new System.Drawing.Point(10, 71);
            this.pathToOriginalImage_input.Name = "pathToOriginalImage_input";
            this.pathToOriginalImage_input.Size = new System.Drawing.Size(173, 20);
            this.pathToOriginalImage_input.TabIndex = 1;
            this.pathToOriginalImage_input.Text = "E:\\Mark\\img\\army\\002.jpg";
            this.pathToOriginalImage_input.Leave += new System.EventHandler(this.PathToOriginalImage_input_Leave);
            // 
            // pathToOriginalImage_button
            // 
            this.pathToOriginalImage_button.Location = new System.Drawing.Point(190, 71);
            this.pathToOriginalImage_button.Name = "pathToOriginalImage_button";
            this.pathToOriginalImage_button.Size = new System.Drawing.Size(100, 23);
            this.pathToOriginalImage_button.TabIndex = 8;
            this.pathToOriginalImage_button.Text = "Select...";
            this.pathToOriginalImage_button.UseVisualStyleBackColor = true;
            this.pathToOriginalImage_button.Click += new System.EventHandler(this.PathToOriginalImage_button_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Original image:";
            // 
            // imageWidth_input
            // 
            this.imageWidth_input.Location = new System.Drawing.Point(10, 116);
            this.imageWidth_input.Name = "imageWidth_input";
            this.imageWidth_input.Size = new System.Drawing.Size(66, 20);
            this.imageWidth_input.TabIndex = 1;
            this.imageWidth_input.Text = "250";
            this.imageWidth_input.Leave += new System.EventHandler(this.ImageWidth_input_Leave);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 99);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Image width:";
            // 
            // imageHeight_input
            // 
            this.imageHeight_input.Location = new System.Drawing.Point(10, 162);
            this.imageHeight_input.Name = "imageHeight_input";
            this.imageHeight_input.Size = new System.Drawing.Size(66, 20);
            this.imageHeight_input.TabIndex = 1;
            this.imageHeight_input.Text = "250";
            this.imageHeight_input.Leave += new System.EventHandler(this.ImageHeight_input_Leave);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 145);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Image height:";
            // 
            // pixelCount_input
            // 
            this.pixelCount_input.Location = new System.Drawing.Point(89, 116);
            this.pixelCount_input.Name = "pixelCount_input";
            this.pixelCount_input.Size = new System.Drawing.Size(66, 20);
            this.pixelCount_input.TabIndex = 1;
            this.pixelCount_input.Text = "50";
            this.pixelCount_input.Leave += new System.EventHandler(this.PixelCount_input_Leave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(89, 99);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 0;
            this.label4.Text = "Pixel count:";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 450);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pathToImagesFolder_label);
            this.Controls.Add(this.process_button);
            this.Controls.Add(this.outputImageBox);
            this.Controls.Add(this.pathToOriginalImage_button);
            this.Controls.Add(this.pathToImagesFolder_button);
            this.Controls.Add(this.pixelCount_input);
            this.Controls.Add(this.imageHeight_input);
            this.Controls.Add(this.imageWidth_input);
            this.Controls.Add(this.pathToOriginalImage_input);
            this.Controls.Add(this.pathToImagesFolder_input);
            this.Name = "Main";
            this.Text = "Image mosaic";
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.FolderBrowserDialog imageFolderBrowserDialog;
        private System.Windows.Forms.TextBox imageHeight_input;
        private System.Windows.Forms.TextBox imageWidth_input;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.OpenFileDialog originalImageBrowserDialog;
        private System.Windows.Forms.PictureBox outputImageBox;
        private System.Windows.Forms.Button pathToImagesFolder_button;
        private System.Windows.Forms.TextBox pathToImagesFolder_input;
        private System.Windows.Forms.Label pathToImagesFolder_label;
        private System.Windows.Forms.Button pathToOriginalImage_button;
        private System.Windows.Forms.TextBox pathToOriginalImage_input;
        private System.Windows.Forms.TextBox pixelCount_input;
        private System.Windows.Forms.Button process_button;

        #endregion
    }
}

