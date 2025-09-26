namespace OfdrwNet.WinFormsDemo
{
    partial class OfdViewerForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomOutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoomFitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripBtnOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripBtnPrevPage = new System.Windows.Forms.ToolStripButton();
            this.toolStripTxtPageNum = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLblPageTotal = new System.Windows.Forms.ToolStripLabel();
            this.toolStripBtnNextPage = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripBtnZoomIn = new System.Windows.Forms.ToolStripButton();
            this.toolStripBtnZoomOut = new System.Windows.Forms.ToolStripButton();
            this.toolStripBtnZoomFit = new System.Windows.Forms.ToolStripButton();
            this.toolStripLblZoom = new System.Windows.Forms.ToolStripLabel();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.splitContainerLeft = new System.Windows.Forms.SplitContainer();
            this.grpPageList = new System.Windows.Forms.GroupBox();
            this.listBoxPages = new System.Windows.Forms.ListBox();
            this.grpDocInfo = new System.Windows.Forms.GroupBox();
            this.txtDocumentInfo = new System.Windows.Forms.TextBox();
            this.panelViewPort = new System.Windows.Forms.Panel();
            this.pictureBoxPage = new System.Windows.Forms.PictureBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip.SuspendLayout();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeft)).BeginInit();
            this.splitContainerLeft.Panel1.SuspendLayout();
            this.splitContainerLeft.Panel2.SuspendLayout();
            this.splitContainerLeft.SuspendLayout();
            this.grpPageList.SuspendLayout();
            this.grpDocInfo.SuspendLayout();
            this.panelViewPort.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPage)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // menuStrip
            //
            this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1200, 28);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            //
            // fileToolStripMenuItem
            //
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(54, 24);
            this.fileToolStripMenuItem.Text = "文件";
            //
            // openToolStripMenuItem
            //
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(177, 26);
            this.openToolStripMenuItem.Text = "打开";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(174, 6);
            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(177, 26);
            this.exitToolStripMenuItem.Text = "退出";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            //
            // viewToolStripMenuItem
            //
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zoomInToolStripMenuItem,
            this.zoomOutToolStripMenuItem,
            this.zoomFitToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(54, 24);
            this.viewToolStripMenuItem.Text = "查看";
            //
            // zoomInToolStripMenuItem
            //
            this.zoomInToolStripMenuItem.Name = "zoomInToolStripMenuItem";
            this.zoomInToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemplus)));
            this.zoomInToolStripMenuItem.Size = new System.Drawing.Size(205, 26);
            this.zoomInToolStripMenuItem.Text = "放大";
            this.zoomInToolStripMenuItem.Click += new System.EventHandler(this.ZoomInToolStripMenuItem_Click);
            //
            // zoomOutToolStripMenuItem
            //
            this.zoomOutToolStripMenuItem.Name = "zoomOutToolStripMenuItem";
            this.zoomOutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.OemMinus)));
            this.zoomOutToolStripMenuItem.Size = new System.Drawing.Size(205, 26);
            this.zoomOutToolStripMenuItem.Text = "缩小";
            this.zoomOutToolStripMenuItem.Click += new System.EventHandler(this.ZoomOutToolStripMenuItem_Click);
            //
            // zoomFitToolStripMenuItem
            //
            this.zoomFitToolStripMenuItem.Name = "zoomFitToolStripMenuItem";
            this.zoomFitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D0)));
            this.zoomFitToolStripMenuItem.Size = new System.Drawing.Size(205, 26);
            this.zoomFitToolStripMenuItem.Text = "适应窗口";
            this.zoomFitToolStripMenuItem.Click += new System.EventHandler(this.ZoomFitToolStripMenuItem_Click);
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripBtnOpen,
            this.toolStripSeparator2,
            this.toolStripBtnPrevPage,
            this.toolStripTxtPageNum,
            this.toolStripLblPageTotal,
            this.toolStripBtnNextPage,
            this.toolStripSeparator3,
            this.toolStripBtnZoomIn,
            this.toolStripBtnZoomOut,
            this.toolStripBtnZoomFit,
            this.toolStripLblZoom});
            this.toolStrip.Location = new System.Drawing.Point(0, 28);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1200, 27);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip";
            //
            // toolStripBtnOpen
            //
            this.toolStripBtnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnOpen.Name = "toolStripBtnOpen";
            this.toolStripBtnOpen.Size = new System.Drawing.Size(43, 24);
            this.toolStripBtnOpen.Text = "打开";
            this.toolStripBtnOpen.Click += new System.EventHandler(this.ToolStripBtnOpen_Click);
            //
            // toolStripSeparator2
            //
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            //
            // toolStripBtnPrevPage
            //
            this.toolStripBtnPrevPage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnPrevPage.Enabled = false;
            this.toolStripBtnPrevPage.Name = "toolStripBtnPrevPage";
            this.toolStripBtnPrevPage.Size = new System.Drawing.Size(58, 24);
            this.toolStripBtnPrevPage.Text = "上一页";
            this.toolStripBtnPrevPage.Click += new System.EventHandler(this.ToolStripBtnPrevPage_Click);
            //
            // toolStripTxtPageNum
            //
            this.toolStripTxtPageNum.BackColor = System.Drawing.SystemColors.Window;
            this.toolStripTxtPageNum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.toolStripTxtPageNum.Name = "toolStripTxtPageNum";
            this.toolStripTxtPageNum.Size = new System.Drawing.Size(50, 27);
            this.toolStripTxtPageNum.TextBoxTextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolStripTxtPageNum.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ToolStripTxtPageNum_KeyPress);
            //
            // toolStripLblPageTotal
            //
            this.toolStripLblPageTotal.Name = "toolStripLblPageTotal";
            this.toolStripLblPageTotal.Size = new System.Drawing.Size(27, 24);
            this.toolStripLblPageTotal.Text = "/ 0";
            //
            // toolStripBtnNextPage
            //
            this.toolStripBtnNextPage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnNextPage.Enabled = false;
            this.toolStripBtnNextPage.Name = "toolStripBtnNextPage";
            this.toolStripBtnNextPage.Size = new System.Drawing.Size(58, 24);
            this.toolStripBtnNextPage.Text = "下一页";
            this.toolStripBtnNextPage.Click += new System.EventHandler(this.ToolStripBtnNextPage_Click);
            //
            // toolStripSeparator3
            //
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 27);
            //
            // toolStripBtnZoomIn
            //
            this.toolStripBtnZoomIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnZoomIn.Enabled = false;
            this.toolStripBtnZoomIn.Name = "toolStripBtnZoomIn";
            this.toolStripBtnZoomIn.Size = new System.Drawing.Size(43, 24);
            this.toolStripBtnZoomIn.Text = "放大";
            this.toolStripBtnZoomIn.Click += new System.EventHandler(this.ToolStripBtnZoomIn_Click);
            //
            // toolStripBtnZoomOut
            //
            this.toolStripBtnZoomOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnZoomOut.Enabled = false;
            this.toolStripBtnZoomOut.Name = "toolStripBtnZoomOut";
            this.toolStripBtnZoomOut.Size = new System.Drawing.Size(43, 24);
            this.toolStripBtnZoomOut.Text = "缩小";
            this.toolStripBtnZoomOut.Click += new System.EventHandler(this.ToolStripBtnZoomOut_Click);
            //
            // toolStripBtnZoomFit
            //
            this.toolStripBtnZoomFit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripBtnZoomFit.Enabled = false;
            this.toolStripBtnZoomFit.Name = "toolStripBtnZoomFit";
            this.toolStripBtnZoomFit.Size = new System.Drawing.Size(73, 24);
            this.toolStripBtnZoomFit.Text = "适应窗口";
            this.toolStripBtnZoomFit.Click += new System.EventHandler(this.ToolStripBtnZoomFit_Click);
            //
            // toolStripLblZoom
            //
            this.toolStripLblZoom.Name = "toolStripLblZoom";
            this.toolStripLblZoom.Size = new System.Drawing.Size(49, 24);
            this.toolStripLblZoom.Text = "100%";
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 55);
            this.splitContainer.Name = "splitContainer";
            //
            // splitContainer.Panel1
            //
            this.splitContainer.Panel1.Controls.Add(this.splitContainerLeft);
            //
            // splitContainer.Panel2
            //
            this.splitContainer.Panel2.Controls.Add(this.panelViewPort);
            this.splitContainer.Size = new System.Drawing.Size(1200, 563);
            this.splitContainer.SplitterDistance = 300;
            this.splitContainer.TabIndex = 2;
            //
            // splitContainerLeft
            //
            this.splitContainerLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerLeft.Location = new System.Drawing.Point(0, 0);
            this.splitContainerLeft.Name = "splitContainerLeft";
            this.splitContainerLeft.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //
            // splitContainerLeft.Panel1
            //
            this.splitContainerLeft.Panel1.Controls.Add(this.grpPageList);
            //
            // splitContainerLeft.Panel2
            //
            this.splitContainerLeft.Panel2.Controls.Add(this.grpDocInfo);
            this.splitContainerLeft.Size = new System.Drawing.Size(300, 563);
            this.splitContainerLeft.SplitterDistance = 280;
            this.splitContainerLeft.TabIndex = 0;
            //
            // grpPageList
            //
            this.grpPageList.Controls.Add(this.listBoxPages);
            this.grpPageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpPageList.Location = new System.Drawing.Point(0, 0);
            this.grpPageList.Name = "grpPageList";
            this.grpPageList.Size = new System.Drawing.Size(300, 280);
            this.grpPageList.TabIndex = 0;
            this.grpPageList.TabStop = false;
            this.grpPageList.Text = "页面列表";
            //
            // listBoxPages
            //
            this.listBoxPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxPages.FormattingEnabled = true;
            this.listBoxPages.ItemHeight = 20;
            this.listBoxPages.Location = new System.Drawing.Point(3, 23);
            this.listBoxPages.Name = "listBoxPages";
            this.listBoxPages.Size = new System.Drawing.Size(294, 254);
            this.listBoxPages.TabIndex = 0;
            this.listBoxPages.SelectedIndexChanged += new System.EventHandler(this.ListBoxPages_SelectedIndexChanged);
            //
            // grpDocInfo
            //
            this.grpDocInfo.Controls.Add(this.txtDocumentInfo);
            this.grpDocInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDocInfo.Location = new System.Drawing.Point(0, 0);
            this.grpDocInfo.Name = "grpDocInfo";
            this.grpDocInfo.Size = new System.Drawing.Size(300, 279);
            this.grpDocInfo.TabIndex = 0;
            this.grpDocInfo.TabStop = false;
            this.grpDocInfo.Text = "文档信息";
            //
            // txtDocumentInfo
            //
            this.txtDocumentInfo.BackColor = System.Drawing.SystemColors.Info;
            this.txtDocumentInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDocumentInfo.Location = new System.Drawing.Point(3, 23);
            this.txtDocumentInfo.Multiline = true;
            this.txtDocumentInfo.Name = "txtDocumentInfo";
            this.txtDocumentInfo.ReadOnly = true;
            this.txtDocumentInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDocumentInfo.Size = new System.Drawing.Size(294, 253);
            this.txtDocumentInfo.TabIndex = 0;
            this.txtDocumentInfo.Text = "请打开 OFD 文档...";
            //
            // panelViewPort
            //
            this.panelViewPort.AutoScroll = true;
            this.panelViewPort.BackColor = System.Drawing.Color.LightGray;
            this.panelViewPort.Controls.Add(this.pictureBoxPage);
            this.panelViewPort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelViewPort.Location = new System.Drawing.Point(0, 0);
            this.panelViewPort.Name = "panelViewPort";
            this.panelViewPort.Size = new System.Drawing.Size(896, 563);
            this.panelViewPort.TabIndex = 0;
            //
            // pictureBoxPage
            //
            this.pictureBoxPage.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureBoxPage.BackColor = System.Drawing.Color.White;
            this.pictureBoxPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxPage.Location = new System.Drawing.Point(50, 50);
            this.pictureBoxPage.Name = "pictureBoxPage";
            this.pictureBoxPage.Size = new System.Drawing.Size(600, 800);
            this.pictureBoxPage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPage.TabIndex = 0;
            this.pictureBoxPage.TabStop = false;
            //
            // statusStrip
            //
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel,
            this.toolStripProgressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 618);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 26);
            this.statusStrip.TabIndex = 3;
            this.statusStrip.Text = "statusStrip";
            //
            // toolStripStatusLabel
            //
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(69, 20);
            this.toolStripStatusLabel.Text = "准备就绪";
            //
            // toolStripProgressBar
            //
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(100, 18);
            this.toolStripProgressBar.Visible = false;
            //
            // openFileDialog
            //
            this.openFileDialog.DefaultExt = "ofd";
            this.openFileDialog.Filter = "OFD文档 (*.ofd)|*.ofd|所有文件 (*.*)|*.*";
            this.openFileDialog.Title = "选择要打开的OFD文档";
            //
            // OfdViewerForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 644);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "OfdViewerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OFD 文档查看器";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.OfdViewerForm_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.splitContainerLeft.Panel1.ResumeLayout(false);
            this.splitContainerLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeft)).EndInit();
            this.splitContainerLeft.ResumeLayout(false);
            this.grpPageList.ResumeLayout(false);
            this.grpDocInfo.ResumeLayout(false);
            this.grpDocInfo.PerformLayout();
            this.panelViewPort.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPage)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomInToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomOutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoomFitToolStripMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripBtnOpen;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripBtnPrevPage;
        private System.Windows.Forms.ToolStripTextBox toolStripTxtPageNum;
        private System.Windows.Forms.ToolStripLabel toolStripLblPageTotal;
        private System.Windows.Forms.ToolStripButton toolStripBtnNextPage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton toolStripBtnZoomIn;
        private System.Windows.Forms.ToolStripButton toolStripBtnZoomOut;
        private System.Windows.Forms.ToolStripButton toolStripBtnZoomFit;
        private System.Windows.Forms.ToolStripLabel toolStripLblZoom;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.SplitContainer splitContainerLeft;
        private System.Windows.Forms.GroupBox grpPageList;
        private System.Windows.Forms.ListBox listBoxPages;
        private System.Windows.Forms.GroupBox grpDocInfo;
        private System.Windows.Forms.TextBox txtDocumentInfo;
        private System.Windows.Forms.Panel panelViewPort;
        private System.Windows.Forms.PictureBox pictureBoxPage;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
    }
}
