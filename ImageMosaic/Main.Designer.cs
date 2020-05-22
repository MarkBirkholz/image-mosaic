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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.mainPage = new System.Windows.Forms.TabPage();
            this.logsPage = new System.Windows.Forms.TabPage();
            this.logBox = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.outputImageBox)).BeginInit();
            this.tabControl.SuspendLayout();
            this.mainPage.SuspendLayout();
            this.logsPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // pathToImagesFolder_label
            // 
            this.pathToImagesFolder_label.AutoSize = true;
            this.pathToImagesFolder_label.Location = new System.Drawing.Point(6, 12);
            this.pathToImagesFolder_label.Name = "pathToImagesFolder_label";
            this.pathToImagesFolder_label.Size = new System.Drawing.Size(109, 13);
            this.pathToImagesFolder_label.TabIndex = 0;
            this.pathToImagesFolder_label.Text = "Path to images folder:";
            // 
            // pathToImagesFolder_input
            // 
            this.pathToImagesFolder_input.Location = new System.Drawing.Point(6, 29);
            this.pathToImagesFolder_input.Name = "pathToImagesFolder_input";
            this.pathToImagesFolder_input.Size = new System.Drawing.Size(173, 20);
            this.pathToImagesFolder_input.TabIndex = 1;
            this.pathToImagesFolder_input.Text = "D:\\Mark\\Files\\img";
            this.pathToImagesFolder_input.Leave += new System.EventHandler(this.PathToImagesFolder_input_Leave);
            // 
            // pathToImagesFolder_button
            // 
            this.pathToImagesFolder_button.Location = new System.Drawing.Point(186, 29);
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
            this.process_button.Location = new System.Drawing.Point(9, 245);
            this.process_button.Name = "process_button";
            this.process_button.Size = new System.Drawing.Size(134, 44);
            this.process_button.TabIndex = 3;
            this.process_button.Text = "GO";
            this.process_button.UseVisualStyleBackColor = true;
            this.process_button.Click += new System.EventHandler(this.process_button_Click);
            // 
            // outputImageBox
            // 
            this.outputImageBox.Location = new System.Drawing.Point(161, 69);
            this.outputImageBox.Name = "outputImageBox";
            this.outputImageBox.Size = new System.Drawing.Size(100, 50);
            this.outputImageBox.TabIndex = 4;
            this.outputImageBox.TabStop = false;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.mainPage);
            this.tabControl.Controls.Add(this.logsPage);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(508, 426);
            this.tabControl.TabIndex = 5;
            // 
            // mainPage
            // 
            this.mainPage.Controls.Add(this.pathToImagesFolder_label);
            this.mainPage.Controls.Add(this.process_button);
            this.mainPage.Controls.Add(this.outputImageBox);
            this.mainPage.Controls.Add(this.pathToImagesFolder_input);
            this.mainPage.Controls.Add(this.pathToImagesFolder_button);
            this.mainPage.Location = new System.Drawing.Point(4, 22);
            this.mainPage.Name = "mainPage";
            this.mainPage.Padding = new System.Windows.Forms.Padding(3);
            this.mainPage.Size = new System.Drawing.Size(500, 400);
            this.mainPage.TabIndex = 0;
            this.mainPage.Text = "Main";
            this.mainPage.UseVisualStyleBackColor = true;
            // 
            // logsPage
            // 
            this.logsPage.Controls.Add(this.logBox);
            this.logsPage.Location = new System.Drawing.Point(4, 22);
            this.logsPage.Name = "logsPage";
            this.logsPage.Padding = new System.Windows.Forms.Padding(3);
            this.logsPage.Size = new System.Drawing.Size(500, 400);
            this.logsPage.TabIndex = 1;
            this.logsPage.Text = "Logs";
            this.logsPage.UseVisualStyleBackColor = true;
            // 
            // logBox
            // 
            this.logBox.Location = new System.Drawing.Point(7, 7);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.ReadOnly = true;
            this.logBox.Size = new System.Drawing.Size(487, 387);
            this.logBox.TabIndex = 0;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(528, 450);
            this.Controls.Add(this.tabControl);
            this.Name = "Main";
            this.Text = "Image mosaic";
            ((System.ComponentModel.ISupportInitialize)(this.outputImageBox)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.mainPage.ResumeLayout(false);
            this.mainPage.PerformLayout();
            this.logsPage.ResumeLayout(false);
            this.logsPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label pathToImagesFolder_label;
        private System.Windows.Forms.TextBox pathToImagesFolder_input;
        private System.Windows.Forms.Button pathToImagesFolder_button;
        private System.Windows.Forms.FolderBrowserDialog imageFolderBrowserDialog;
        private System.Windows.Forms.Button process_button;
        private System.Windows.Forms.PictureBox outputImageBox;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage mainPage;
        private System.Windows.Forms.TabPage logsPage;
        private System.Windows.Forms.TextBox logBox;
    }
}

