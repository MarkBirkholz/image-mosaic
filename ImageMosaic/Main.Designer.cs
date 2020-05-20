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
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pathToImagesFolder_label = new System.Windows.Forms.Label();
            this.pathToImagesFolder_input = new System.Windows.Forms.TextBox();
            this.pathToImagesFolder_button = new System.Windows.Forms.Button();
            this.imageFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.process_button = new System.Windows.Forms.Button();
            this.outputImageBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.outputImageBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pathToImagesFolder_label
            // 
            this.pathToImagesFolder_label.AutoSize = true;
            this.pathToImagesFolder_label.Location = new System.Drawing.Point(13, 13);
            this.pathToImagesFolder_label.Name = "pathToImagesFolder_label";
            this.pathToImagesFolder_label.Size = new System.Drawing.Size(109, 13);
            this.pathToImagesFolder_label.TabIndex = 0;
            this.pathToImagesFolder_label.Text = "Path to images folder:";
            // 
            // pathToImagesFolder_input
            // 
            this.pathToImagesFolder_input.Location = new System.Drawing.Point(13, 30);
            this.pathToImagesFolder_input.Name = "pathToImagesFolder_input";
            this.pathToImagesFolder_input.Size = new System.Drawing.Size(173, 20);
            this.pathToImagesFolder_input.TabIndex = 1;
            this.pathToImagesFolder_input.Leave += new System.EventHandler(this.PathToImagesFolder_input_Leave);
            // 
            // pathToImagesFolder_button
            // 
            this.pathToImagesFolder_button.Location = new System.Drawing.Point(193, 30);
            this.pathToImagesFolder_button.Name = "pathToImagesFolder_button";
            this.pathToImagesFolder_button.Size = new System.Drawing.Size(75, 19);
            this.pathToImagesFolder_button.TabIndex = 2;
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
            this.process_button.Location = new System.Drawing.Point(16, 394);
            this.process_button.Name = "process_button";
            this.process_button.Size = new System.Drawing.Size(134, 44);
            this.process_button.TabIndex = 3;
            this.process_button.Text = "GO";
            this.process_button.UseVisualStyleBackColor = true;
            this.process_button.Click += new System.EventHandler(this.process_button_Click);
            // 
            // outputImageBox
            // 
            this.outputImageBox.Location = new System.Drawing.Point(168, 70);
            this.outputImageBox.Name = "outputImageBox";
            this.outputImageBox.Size = new System.Drawing.Size(100, 50);
            this.outputImageBox.TabIndex = 4;
            this.outputImageBox.TabStop = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.outputImageBox);
            this.Controls.Add(this.process_button);
            this.Controls.Add(this.pathToImagesFolder_button);
            this.Controls.Add(this.pathToImagesFolder_input);
            this.Controls.Add(this.pathToImagesFolder_label);
            this.Name = "Main";
            this.Text = "Image mosaic";
            ((System.ComponentModel.ISupportInitialize)(this.outputImageBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label pathToImagesFolder_label;
        private System.Windows.Forms.TextBox pathToImagesFolder_input;
        private System.Windows.Forms.Button pathToImagesFolder_button;
        private System.Windows.Forms.FolderBrowserDialog imageFolderBrowserDialog;
        private System.Windows.Forms.Button process_button;
        private System.Windows.Forms.PictureBox outputImageBox;
    }
}

