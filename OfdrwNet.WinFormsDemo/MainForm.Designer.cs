namespace OfdrwNet.WinFormsDemo
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grpInputFile = new System.Windows.Forms.GroupBox();
            this.btnBrowseInput = new System.Windows.Forms.Button();
            this.txtInputFile = new System.Windows.Forms.TextBox();
            this.lblInputFile = new System.Windows.Forms.Label();
            this.grpOutputFile = new System.Windows.Forms.GroupBox();
            this.btnBrowseOutput = new System.Windows.Forms.Button();
            this.txtOutputFile = new System.Windows.Forms.TextBox();
            this.lblOutputFile = new System.Windows.Forms.Label();
            this.grpConversionType = new System.Windows.Forms.GroupBox();
            this.rbPdfToOfd = new System.Windows.Forms.RadioButton();
            this.rbHtmlToOfd = new System.Windows.Forms.RadioButton();
            this.rbWordToOfd = new System.Windows.Forms.RadioButton();
            this.grpProgress = new System.Windows.Forms.GroupBox();
            this.lblProgressMessage = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnConvert = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnViewOfd = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.grpFileInfo = new System.Windows.Forms.GroupBox();
            this.txtFileInfo = new System.Windows.Forms.TextBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.grpInputFile.SuspendLayout();
            this.grpOutputFile.SuspendLayout();
            this.grpConversionType.SuspendLayout();
            this.grpProgress.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.grpFileInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblTitle.Location = new System.Drawing.Point(10, 10);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(764, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "OFDRW.NET 文档转换工具";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // grpInputFile
            // 
            this.grpInputFile.Controls.Add(this.btnBrowseInput);
            this.grpInputFile.Controls.Add(this.txtInputFile);
            this.grpInputFile.Controls.Add(this.lblInputFile);
            this.grpInputFile.Location = new System.Drawing.Point(10, 60);
            this.grpInputFile.Name = "grpInputFile";
            this.grpInputFile.Size = new System.Drawing.Size(764, 70);
            this.grpInputFile.TabIndex = 1;
            this.grpInputFile.TabStop = false;
            this.grpInputFile.Text = "输入文件";
            // 
            // btnBrowseInput
            // 
            this.btnBrowseInput.Location = new System.Drawing.Point(670, 25);
            this.btnBrowseInput.Name = "btnBrowseInput";
            this.btnBrowseInput.Size = new System.Drawing.Size(80, 30);
            this.btnBrowseInput.TabIndex = 2;
            this.btnBrowseInput.Text = "浏览...";
            this.btnBrowseInput.UseVisualStyleBackColor = true;
            this.btnBrowseInput.Click += new System.EventHandler(this.BtnBrowseInput_Click);
            // 
            // txtInputFile
            // 
            this.txtInputFile.Location = new System.Drawing.Point(80, 28);
            this.txtInputFile.Name = "txtInputFile";
            this.txtInputFile.ReadOnly = true;
            this.txtInputFile.Size = new System.Drawing.Size(580, 23);
            this.txtInputFile.TabIndex = 1;
            this.toolTip.SetToolTip(this.txtInputFile, "选择要转换的文件（支持 .docx, .html, .htm, .pdf 格式）");
            // 
            // lblInputFile
            // 
            this.lblInputFile.AutoSize = true;
            this.lblInputFile.Location = new System.Drawing.Point(15, 31);
            this.lblInputFile.Name = "lblInputFile";
            this.lblInputFile.Size = new System.Drawing.Size(59, 17);
            this.lblInputFile.TabIndex = 0;
            this.lblInputFile.Text = "源文件：";
            // 
            // grpOutputFile
            // 
            this.grpOutputFile.Controls.Add(this.btnBrowseOutput);
            this.grpOutputFile.Controls.Add(this.txtOutputFile);
            this.grpOutputFile.Controls.Add(this.lblOutputFile);
            this.grpOutputFile.Location = new System.Drawing.Point(10, 140);
            this.grpOutputFile.Name = "grpOutputFile";
            this.grpOutputFile.Size = new System.Drawing.Size(764, 70);
            this.grpOutputFile.TabIndex = 2;
            this.grpOutputFile.TabStop = false;
            this.grpOutputFile.Text = "输出文件";
            // 
            // btnBrowseOutput
            // 
            this.btnBrowseOutput.Location = new System.Drawing.Point(670, 25);
            this.btnBrowseOutput.Name = "btnBrowseOutput";
            this.btnBrowseOutput.Size = new System.Drawing.Size(80, 30);
            this.btnBrowseOutput.TabIndex = 2;
            this.btnBrowseOutput.Text = "浏览...";
            this.btnBrowseOutput.UseVisualStyleBackColor = true;
            this.btnBrowseOutput.Click += new System.EventHandler(this.BtnBrowseOutput_Click);
            // 
            // txtOutputFile
            // 
            this.txtOutputFile.Location = new System.Drawing.Point(80, 28);
            this.txtOutputFile.Name = "txtOutputFile";
            this.txtOutputFile.Size = new System.Drawing.Size(580, 23);
            this.txtOutputFile.TabIndex = 1;
            this.toolTip.SetToolTip(this.txtOutputFile, "指定输出的OFD文件路径");
            // 
            // lblOutputFile
            // 
            this.lblOutputFile.AutoSize = true;
            this.lblOutputFile.Location = new System.Drawing.Point(15, 31);
            this.lblOutputFile.Name = "lblOutputFile";
            this.lblOutputFile.Size = new System.Drawing.Size(71, 17);
            this.lblOutputFile.TabIndex = 0;
            this.lblOutputFile.Text = "目标文件：";
            // 
            // grpConversionType
            // 
            this.grpConversionType.Controls.Add(this.rbPdfToOfd);
            this.grpConversionType.Controls.Add(this.rbHtmlToOfd);
            this.grpConversionType.Controls.Add(this.rbWordToOfd);
            this.grpConversionType.Location = new System.Drawing.Point(10, 220);
            this.grpConversionType.Name = "grpConversionType";
            this.grpConversionType.Size = new System.Drawing.Size(764, 60);
            this.grpConversionType.TabIndex = 3;
            this.grpConversionType.TabStop = false;
            this.grpConversionType.Text = "转换类型";
            // 
            // rbPdfToOfd
            // 
            this.rbPdfToOfd.AutoSize = true;
            this.rbPdfToOfd.Location = new System.Drawing.Point(520, 25);
            this.rbPdfToOfd.Name = "rbPdfToOfd";
            this.rbPdfToOfd.Size = new System.Drawing.Size(98, 21);
            this.rbPdfToOfd.TabIndex = 2;
            this.rbPdfToOfd.Text = "PDF → OFD";
            this.rbPdfToOfd.UseVisualStyleBackColor = true;
            // 
            // rbHtmlToOfd
            // 
            this.rbHtmlToOfd.AutoSize = true;
            this.rbHtmlToOfd.Location = new System.Drawing.Point(270, 25);
            this.rbHtmlToOfd.Name = "rbHtmlToOfd";
            this.rbHtmlToOfd.Size = new System.Drawing.Size(110, 21);
            this.rbHtmlToOfd.TabIndex = 1;
            this.rbHtmlToOfd.Text = "HTML → OFD";
            this.rbHtmlToOfd.UseVisualStyleBackColor = true;
            // 
            // rbWordToOfd
            // 
            this.rbWordToOfd.AutoSize = true;
            this.rbWordToOfd.Checked = true;
            this.rbWordToOfd.Location = new System.Drawing.Point(20, 25);
            this.rbWordToOfd.Name = "rbWordToOfd";
            this.rbWordToOfd.Size = new System.Drawing.Size(110, 21);
            this.rbWordToOfd.TabIndex = 0;
            this.rbWordToOfd.TabStop = true;
            this.rbWordToOfd.Text = "Word → OFD";
            this.rbWordToOfd.UseVisualStyleBackColor = true;
            // 
            // grpProgress
            // 
            this.grpProgress.Controls.Add(this.lblProgressMessage);
            this.grpProgress.Controls.Add(this.progressBar);
            this.grpProgress.Location = new System.Drawing.Point(10, 450);
            this.grpProgress.Name = "grpProgress";
            this.grpProgress.Size = new System.Drawing.Size(764, 80);
            this.grpProgress.TabIndex = 5;
            this.grpProgress.TabStop = false;
            this.grpProgress.Text = "转换进度";
            // 
            // lblProgressMessage
            // 
            this.lblProgressMessage.AutoSize = true;
            this.lblProgressMessage.Location = new System.Drawing.Point(15, 25);
            this.lblProgressMessage.Name = "lblProgressMessage";
            this.lblProgressMessage.Size = new System.Drawing.Size(56, 17);
            this.lblProgressMessage.TabIndex = 1;
            this.lblProgressMessage.Text = "准备就绪";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(15, 45);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(735, 23);
            this.progressBar.TabIndex = 0;
            // 
            // btnConvert
            // 
            this.btnConvert.BackColor = System.Drawing.Color.LightGreen;
            this.btnConvert.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnConvert.Location = new System.Drawing.Point(410, 400);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(80, 35);
            this.btnConvert.TabIndex = 6;
            this.btnConvert.Text = "开始转换";
            this.btnConvert.UseVisualStyleBackColor = false;
            this.btnConvert.Click += new System.EventHandler(this.BtnConvert_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.LightCoral;
            this.btnCancel.Enabled = false;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnCancel.Location = new System.Drawing.Point(500, 400);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 35);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "取消转换";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(680, 400);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(80, 35);
            this.btnClear.TabIndex = 8;
            this.btnClear.Text = "清空";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnViewOfd
            // 
            this.btnViewOfd.BackColor = System.Drawing.Color.LightBlue;
            this.btnViewOfd.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F);
            this.btnViewOfd.Location = new System.Drawing.Point(590, 400);
            this.btnViewOfd.Name = "btnViewOfd";
            this.btnViewOfd.Size = new System.Drawing.Size(80, 35);
            this.btnViewOfd.TabIndex = 9;
            this.btnViewOfd.Text = "查看OFD";
            this.btnViewOfd.UseVisualStyleBackColor = false;
            this.btnViewOfd.Click += new System.EventHandler(this.BtnViewOfd_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel,
            this.toolStripProgressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 547);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 9;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(56, 17);
            this.toolStripStatusLabel.Text = "准备就绪";
            // 
            // toolStripProgressBar
            // 
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar.Visible = false;
            // 
            // grpFileInfo
            // 
            this.grpFileInfo.Controls.Add(this.txtFileInfo);
            this.grpFileInfo.Location = new System.Drawing.Point(10, 290);
            this.grpFileInfo.Name = "grpFileInfo";
            this.grpFileInfo.Size = new System.Drawing.Size(764, 100);
            this.grpFileInfo.TabIndex = 4;
            this.grpFileInfo.TabStop = false;
            this.grpFileInfo.Text = "文件信息";
            // 
            // txtFileInfo
            // 
            this.txtFileInfo.BackColor = System.Drawing.SystemColors.Window;
            this.txtFileInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFileInfo.Location = new System.Drawing.Point(3, 19);
            this.txtFileInfo.Multiline = true;
            this.txtFileInfo.Name = "txtFileInfo";
            this.txtFileInfo.ReadOnly = true;
            this.txtFileInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFileInfo.Size = new System.Drawing.Size(758, 78);
            this.txtFileInfo.TabIndex = 0;
            this.txtFileInfo.Text = "请选择要转换的文件...";
            // 
            // openFileDialog
            // 
            this.openFileDialog.Title = "选择要转换的文件";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "ofd";
            this.saveFileDialog.Filter = "OFD文件 (*.ofd)|*.ofd|所有文件 (*.*)|*.*";
            this.saveFileDialog.Title = "保存OFD文件";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 569);
            this.Controls.Add(this.grpFileInfo);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnViewOfd);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.grpProgress);
            this.Controls.Add(this.grpConversionType);
            this.Controls.Add(this.grpOutputFile);
            this.Controls.Add(this.grpInputFile);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OFDRW.NET 文档转换工具 v1.0";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.grpInputFile.ResumeLayout(false);
            this.grpInputFile.PerformLayout();
            this.grpOutputFile.ResumeLayout(false);
            this.grpOutputFile.PerformLayout();
            this.grpConversionType.ResumeLayout(false);
            this.grpConversionType.PerformLayout();
            this.grpProgress.ResumeLayout(false);
            this.grpProgress.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.grpFileInfo.ResumeLayout(false);
            this.grpFileInfo.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox grpInputFile;
        private System.Windows.Forms.Button btnBrowseInput;
        private System.Windows.Forms.TextBox txtInputFile;
        private System.Windows.Forms.Label lblInputFile;
        private System.Windows.Forms.GroupBox grpOutputFile;
        private System.Windows.Forms.Button btnBrowseOutput;
        private System.Windows.Forms.TextBox txtOutputFile;
        private System.Windows.Forms.Label lblOutputFile;
        private System.Windows.Forms.GroupBox grpConversionType;
        private System.Windows.Forms.RadioButton rbPdfToOfd;
        private System.Windows.Forms.RadioButton rbHtmlToOfd;
        private System.Windows.Forms.RadioButton rbWordToOfd;
        private System.Windows.Forms.GroupBox grpProgress;
        private System.Windows.Forms.Label lblProgressMessage;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnViewOfd;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.GroupBox grpFileInfo;
        private System.Windows.Forms.TextBox txtFileInfo;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolTip toolTip;
    }
}