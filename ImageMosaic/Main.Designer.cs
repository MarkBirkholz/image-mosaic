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
            this.cellSizeValue_label = new System.Windows.Forms.Label();
            this.cellSize = new System.Windows.Forms.TrackBar();
            this.imageBox = new System.Windows.Forms.GroupBox();
            this.cellSize_label = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.cellSize)).BeginInit();
            this.imageBox.SuspendLayout();
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
            this.originalImageBrowserDialog.Filter = "Images|*.jpg;*.jpeg;*.png";
            this.originalImageBrowserDialog.InitialDirectory = "E:\\Mark\\Files\\img";
            // 
            // process_button
            // 
            this.process_button.Location = new System.Drawing.Point(10, 166);
            this.process_button.Name = "process_button";
            this.process_button.Size = new System.Drawing.Size(134, 44);
            this.process_button.TabIndex = 3;
            this.process_button.Text = "GO";
            this.process_button.UseVisualStyleBackColor = true;
            this.process_button.Click += new System.EventHandler(this.process_button_Click);
            // 
            // outputImageBox
            // 
            this.outputImageBox.Location = new System.Drawing.Point(6, 19);
            this.outputImageBox.Name = "outputImageBox";
            this.outputImageBox.Size = new System.Drawing.Size(100, 50);
            this.outputImageBox.TabIndex = 4;
            this.outputImageBox.TabStop = false;
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(10, 400);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
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
            // cellSizeValue_label
            // 
            this.cellSizeValue_label.AutoSize = true;
            this.cellSizeValue_label.Location = new System.Drawing.Point(184, 116);
            this.cellSizeValue_label.Name = "cellSizeValue_label";
            this.cellSizeValue_label.Size = new System.Drawing.Size(13, 13);
            this.cellSizeValue_label.TabIndex = 0;
            this.cellSizeValue_label.Text = "5";
            // 
            // cellSize
            // 
            this.cellSize.Location = new System.Drawing.Point(10, 116);
            this.cellSize.Maximum = 50;
            this.cellSize.Minimum = 5;
            this.cellSize.Name = "cellSize";
            this.cellSize.Size = new System.Drawing.Size(168, 45);
            this.cellSize.SmallChange = 5;
            this.cellSize.TabIndex = 10;
            this.cellSize.Value = 5;
            this.cellSize.Scroll += new System.EventHandler(this.cellSize_Scroll);
            // 
            // imageBox
            // 
            this.imageBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.imageBox.Controls.Add(this.outputImageBox);
            this.imageBox.Location = new System.Drawing.Point(294, 24);
            this.imageBox.Name = "imageBox";
            this.imageBox.Size = new System.Drawing.Size(478, 370);
            this.imageBox.TabIndex = 9;
            this.imageBox.TabStop = false;
            this.imageBox.Text = "Итоговое изображение";
            // 
            // cellSize_label
            // 
            this.cellSize_label.AutoSize = true;
            this.cellSize_label.Location = new System.Drawing.Point(12, 100);
            this.cellSize_label.Name = "cellSize_label";
            this.cellSize_label.Size = new System.Drawing.Size(48, 13);
            this.cellSize_label.TabIndex = 0;
            this.cellSize_label.Text = "Cell size:";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.cellSize);
            this.Controls.Add(this.imageBox);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.cellSize_label);
            this.Controls.Add(this.cellSizeValue_label);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pathToImagesFolder_label);
            this.Controls.Add(this.process_button);
            this.Controls.Add(this.pathToOriginalImage_button);
            this.Controls.Add(this.pathToImagesFolder_button);
            this.Controls.Add(this.pathToOriginalImage_input);
            this.Controls.Add(this.pathToImagesFolder_input);
            this.Location = new System.Drawing.Point(15, 15);
            this.Name = "Main";
            ((System.ComponentModel.ISupportInitialize) (this.outputImageBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.cellSize)).EndInit();
            this.imageBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TrackBar cellSize;
        private System.Windows.Forms.Label cellSize_label;
        private System.Windows.Forms.Label cellSizeValue_label;
        private System.Windows.Forms.GroupBox imageBox;
        private System.Windows.Forms.FolderBrowserDialog imageFolderBrowserDialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox logBox;
        private System.Windows.Forms.OpenFileDialog originalImageBrowserDialog;
        private System.Windows.Forms.PictureBox outputImageBox;
        private System.Windows.Forms.Button pathToImagesFolder_button;
        private System.Windows.Forms.TextBox pathToImagesFolder_input;
        private System.Windows.Forms.Label pathToImagesFolder_label;
        private System.Windows.Forms.Button pathToOriginalImage_button;
        private System.Windows.Forms.TextBox pathToOriginalImage_input;
        private System.Windows.Forms.Button process_button;

        #endregion
    }
}

