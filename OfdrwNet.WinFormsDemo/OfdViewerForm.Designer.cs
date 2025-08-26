namespace OfdrwNet.WinFormsDemo
{
    partial class OfdViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblTitle = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.grpDocumentInfo = new System.Windows.Forms.GroupBox();
            this.txtDocumentInfo = new System.Windows.Forms.TextBox();
            this.grpPageList = new System.Windows.Forms.GroupBox();
            this.listBoxPages = new System.Windows.Forms.ListBox();
            this.grpPageContent = new System.Windows.Forms.GroupBox();
            this.txtPageContent = new System.Windows.Forms.TextBox();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripBtnOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLblPage = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTxtPageNum = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLblPageTotal = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripBtnPrevPage = new System.Windows.Forms.ToolStripButton();
            this.toolStripBtnNextPage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripBtnClose = new System.Windows.Forms.ToolStripButton();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.grpDocumentInfo.SuspendLayout();
            this.grpPageList.SuspendLayout();
            this.grpPageContent.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.DarkBlue;
            this.lblTitle.Location = new System.Drawing.Point(0, 25);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(1000, 35);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "OFD 文档查看器";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.Location = new System.Drawing.Point(0, 60);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.grpPageList);
            this.splitContainer.Panel1.Controls.Add(this.grpDocumentInfo);
            this.splitContainer.Panel1MinSize = 250;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.grpPageContent);
            this.splitContainer.Size = new System.Drawing.Size(1000, 518);
            this.splitContainer.SplitterDistance = 300;
            this.splitContainer.TabIndex = 1;
            // 
            // grpDocumentInfo
            // 
            this.grpDocumentInfo.Controls.Add(this.txtDocumentInfo);
            this.grpDocumentInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.grpDocumentInfo.Location = new System.Drawing.Point(0, 0);
            this.grpDocumentInfo.Name = "grpDocumentInfo";
            this.grpDocumentInfo.Size = new System.Drawing.Size(300, 200);
            this.grpDocumentInfo.TabIndex = 0;
            this.grpDocumentInfo.TabStop = false;
            this.grpDocumentInfo.Text = "文档信息";
            // 
            // txtDocumentInfo
            // 
            this.txtDocumentInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDocumentInfo.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtDocumentInfo.Location = new System.Drawing.Point(3, 19);
            this.txtDocumentInfo.Multiline = true;
            this.txtDocumentInfo.Name = "txtDocumentInfo";
            this.txtDocumentInfo.ReadOnly = true;
            this.txtDocumentInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDocumentInfo.Size = new System.Drawing.Size(294, 178);
            this.txtDocumentInfo.TabIndex = 0;
            this.txtDocumentInfo.Text = "请打开 OFD 文档...";
            // 
            // grpPageList
            // 
            this.grpPageList.Controls.Add(this.listBoxPages);
            this.grpPageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPageList.Location = new System.Drawing.Point(0, 200);
            this.grpPageList.Name = "grpPageList";
            this.grpPageList.Size = new System.Drawing.Size(300, 318);
            this.grpPageList.TabIndex = 1;
            this.grpPageList.TabStop = false;
            this.grpPageList.Text = "页面列表";
            // 
            // listBoxPages
            // 
            this.listBoxPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxPages.FormattingEnabled = true;
            this.listBoxPages.ItemHeight = 17;
            this.listBoxPages.Location = new System.Drawing.Point(3, 19);
            this.listBoxPages.Name = "listBoxPages";
            this.listBoxPages.Size = new System.Drawing.Size(294, 296);
            this.listBoxPages.TabIndex = 0;
            this.listBoxPages.SelectedIndexChanged += new System.EventHandler(this.ListBoxPages_SelectedIndexChanged);
            // 
            // grpPageContent
            // 
            this.grpPageContent.Controls.Add(this.txtPageContent);
            this.grpPageContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPageContent.Location = new System.Drawing.Point(0, 0);
            this.grpPageContent.Name = "grpPageContent";
            this.grpPageContent.Size = new System.Drawing.Size(696, 518);
            this.grpPageContent.TabIndex = 0;
            this.grpPageContent.TabStop = false;
            this.grpPageContent.Text = "页面内容";
            // 
            // txtPageContent
            // 
            this.txtPageContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPageContent.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtPageContent.Location = new System.Drawing.Point(3, 19);
            this.txtPageContent.Multiline = true;
            this.txtPageContent.Name = "txtPageContent";
            this.txtPageContent.ReadOnly = true;
            this.txtPageContent.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPageContent.Size = new System.Drawing.Size(690, 496);
            this.txtPageContent.TabIndex = 0;
            this.txtPageContent.Text = "请选择页面查看内容...";
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripBtnOpen,
            this.toolStripSeparator1,
            this.toolStripLblPage,
            this.toolStripTxtPageNum,
            this.toolStripLblPageTotal,
            this.toolStripSeparator2,
            this.toolStripBtnPrevPage,
            this.toolStripBtnNextPage,
            this.toolStripSeparator3,
            this.toolStripBtnClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolStripBtnOpen
            // 
            this.toolStripBtnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnOpen.Name = "toolStripBtnOpen";
            this.toolStripBtnOpen.Size = new System.Drawing.Size(36, 22);
            this.toolStripBtnOpen.Text = "打开";
            this.toolStripBtnOpen.Click += new System.EventHandler(this.ToolStripBtnOpen_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLblPage
            // 
            this.toolStripLblPage.Name = "toolStripLblPage";
            this.toolStripLblPage.Size = new System.Drawing.Size(32, 22);
            this.toolStripLblPage.Text = "页面:";
            // 
            // toolStripTxtPageNum
            // 
            this.toolStripTxtPageNum.Name = "toolStripTxtPageNum";
            this.toolStripTxtPageNum.Size = new System.Drawing.Size(50, 25);
            this.toolStripTxtPageNum.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolStripTxtPageNum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ToolStripTxtPageNum_KeyPress);
            // 
            // toolStripLblPageTotal
            // 
            this.toolStripLblPageTotal.Name = "toolStripLblPageTotal";
            this.toolStripLblPageTotal.Size = new System.Drawing.Size(20, 22);
            this.toolStripLblPageTotal.Text = "/ 0";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripBtnPrevPage
            // 
            this.toolStripBtnPrevPage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnPrevPage.Enabled = false;
            this.toolStripBtnPrevPage.Name = "toolStripBtnPrevPage";
            this.toolStripBtnPrevPage.Size = new System.Drawing.Size(36, 22);
            this.toolStripBtnPrevPage.Text = "上页";
            this.toolStripBtnPrevPage.Click += new System.EventHandler(this.ToolStripBtnPrevPage_Click);
            // 
            // toolStripBtnNextPage
            // 
            this.toolStripBtnNextPage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnNextPage.Enabled = false;
            this.toolStripBtnNextPage.Name = "toolStripBtnNextPage";
            this.toolStripBtnNextPage.Size = new System.Drawing.Size(36, 22);
            this.toolStripBtnNextPage.Text = "下页";
            this.toolStripBtnNextPage.Click += new System.EventHandler(this.ToolStripBtnNextPage_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripBtnClose
            // 
            this.toolStripBtnClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnClose.Name = "toolStripBtnClose";
            this.toolStripBtnClose.Size = new System.Drawing.Size(36, 22);
            this.toolStripBtnClose.Text = "关闭";
            this.toolStripBtnClose.Click += new System.EventHandler(this.ToolStripBtnClose_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 578);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1000, 22);
            this.statusStrip.TabIndex = 3;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(56, 17);
            this.toolStripStatusLabel.Text = "准备就绪";
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ofd";
            this.openFileDialog.Filter = "OFD文件 (*.ofd)|*.ofd|所有文件 (*.*)|*.*";
            this.openFileDialog.Title = "打开OFD文档";
            // 
            // OfdViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Name = "OfdViewerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OFD 文档查看器";
            this.Load += new System.EventHandler(this.OfdViewerForm_Load);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.grpDocumentInfo.ResumeLayout(false);
            this.grpDocumentInfo.ResumeLayout(false);
            this.grpDocumentInfo.PerformLayout();
            this.grpPageList.ResumeLayout(false);
            this.grpPageContent.ResumeLayout(false);
            this.grpPageContent.ResumeLayout(false);
            this.grpPageContent.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.GroupBox grpDocumentInfo;
        private System.Windows.Forms.TextBox txtDocumentInfo;
        private System.Windows.Forms.GroupBox grpPageList;
        private System.Windows.Forms.ListBox listBoxPages;
        private System.Windows.Forms.GroupBox grpPageContent;
        private System.Windows.Forms.TextBox txtPageContent;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripBtnOpen;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel toolStripLblPage;
        private System.Windows.Forms.ToolStripTextBox toolStripTxtPageNum;
        private System.Windows.Forms.ToolStripLabel toolStripLblPageTotal;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripBtnPrevPage;
        private System.Windows.Forms.ToolStripButton toolStripBtnNextPage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripBtnClose;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ToolTip toolTip;
    }
}