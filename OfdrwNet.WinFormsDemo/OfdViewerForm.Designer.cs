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
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            zoomInToolStripMenuItem = new ToolStripMenuItem();
            zoomOutToolStripMenuItem = new ToolStripMenuItem();
            zoomFitToolStripMenuItem = new ToolStripMenuItem();
            toolStrip = new ToolStrip();
            toolStripBtnOpen = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripBtnPrevPage = new ToolStripButton();
            toolStripTxtPageNum = new ToolStripTextBox();
            toolStripLblPageTotal = new ToolStripLabel();
            toolStripBtnNextPage = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripBtnZoomIn = new ToolStripButton();
            toolStripBtnZoomOut = new ToolStripButton();
            toolStripBtnZoomFit = new ToolStripButton();
            toolStripLblZoom = new ToolStripLabel();
            splitContainer = new SplitContainer();
            splitContainerLeft = new SplitContainer();
            grpPageList = new GroupBox();
            listBoxPages = new ListBox();
            grpDocInfo = new GroupBox();
            txtDocumentInfo = new TextBox();
            panelViewPort = new Panel();
            statusStrip = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripProgressBar = new ToolStripProgressBar();
            openFileDialog = new OpenFileDialog();
            menuStrip.SuspendLayout();
            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).BeginInit();
            splitContainerLeft.Panel1.SuspendLayout();
            splitContainerLeft.Panel2.SuspendLayout();
            splitContainerLeft.SuspendLayout();
            grpPageList.SuspendLayout();
            grpDocInfo.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 2, 0, 2);
            menuStrip.Size = new Size(1350, 28);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(53, 24);
            fileToolStripMenuItem.Text = "文件";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openToolStripMenuItem.Size = new Size(180, 26);
            openToolStripMenuItem.Text = "打开";
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(180, 26);
            exitToolStripMenuItem.Text = "退出";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { zoomInToolStripMenuItem, zoomOutToolStripMenuItem, zoomFitToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(53, 24);
            viewToolStripMenuItem.Text = "查看";
            // 
            // zoomInToolStripMenuItem
            // 
            zoomInToolStripMenuItem.Name = "zoomInToolStripMenuItem";
            zoomInToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Oemplus;
            zoomInToolStripMenuItem.Size = new Size(247, 26);
            zoomInToolStripMenuItem.Text = "放大";
            zoomInToolStripMenuItem.Click += ZoomInToolStripMenuItem_Click;
            // 
            // zoomOutToolStripMenuItem
            // 
            zoomOutToolStripMenuItem.Name = "zoomOutToolStripMenuItem";
            zoomOutToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.OemMinus;
            zoomOutToolStripMenuItem.Size = new Size(247, 26);
            zoomOutToolStripMenuItem.Text = "缩小";
            zoomOutToolStripMenuItem.Click += ZoomOutToolStripMenuItem_Click;
            // 
            // zoomFitToolStripMenuItem
            // 
            zoomFitToolStripMenuItem.Name = "zoomFitToolStripMenuItem";
            zoomFitToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.D0;
            zoomFitToolStripMenuItem.Size = new Size(247, 26);
            zoomFitToolStripMenuItem.Text = "适应窗口";
            zoomFitToolStripMenuItem.Click += ZoomFitToolStripMenuItem_Click;
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(20, 20);
            toolStrip.Items.AddRange(new ToolStripItem[] { toolStripBtnOpen, toolStripSeparator2, toolStripBtnPrevPage, toolStripTxtPageNum, toolStripLblPageTotal, toolStripBtnNextPage, toolStripSeparator3, toolStripBtnZoomIn, toolStripBtnZoomOut, toolStripBtnZoomFit, toolStripLblZoom });
            toolStrip.Location = new Point(0, 28);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1350, 27);
            toolStrip.TabIndex = 1;
            toolStrip.Text = "toolStrip";
            // 
            // toolStripBtnOpen
            // 
            toolStripBtnOpen.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnOpen.Name = "toolStripBtnOpen";
            toolStripBtnOpen.Size = new Size(43, 24);
            toolStripBtnOpen.Text = "打开";
            toolStripBtnOpen.Click += ToolStripBtnOpen_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 27);
            // 
            // toolStripBtnPrevPage
            // 
            toolStripBtnPrevPage.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnPrevPage.Enabled = false;
            toolStripBtnPrevPage.Name = "toolStripBtnPrevPage";
            toolStripBtnPrevPage.Size = new Size(58, 24);
            toolStripBtnPrevPage.Text = "上一页";
            toolStripBtnPrevPage.Click += ToolStripBtnPrevPage_Click;
            // 
            // toolStripTxtPageNum
            // 
            toolStripTxtPageNum.BackColor = SystemColors.Window;
            toolStripTxtPageNum.BorderStyle = BorderStyle.FixedSingle;
            toolStripTxtPageNum.Name = "toolStripTxtPageNum";
            toolStripTxtPageNum.Size = new Size(56, 27);
            toolStripTxtPageNum.TextBoxTextAlign = HorizontalAlignment.Center;
            toolStripTxtPageNum.KeyPress += ToolStripTxtPageNum_KeyPress;
            // 
            // toolStripLblPageTotal
            // 
            toolStripLblPageTotal.Name = "toolStripLblPageTotal";
            toolStripLblPageTotal.Size = new Size(28, 24);
            toolStripLblPageTotal.Text = "/ 0";
            // 
            // toolStripBtnNextPage
            // 
            toolStripBtnNextPage.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnNextPage.Enabled = false;
            toolStripBtnNextPage.Name = "toolStripBtnNextPage";
            toolStripBtnNextPage.Size = new Size(58, 24);
            toolStripBtnNextPage.Text = "下一页";
            toolStripBtnNextPage.Click += ToolStripBtnNextPage_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 27);
            // 
            // toolStripBtnZoomIn
            // 
            toolStripBtnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnZoomIn.Enabled = false;
            toolStripBtnZoomIn.Name = "toolStripBtnZoomIn";
            toolStripBtnZoomIn.Size = new Size(43, 24);
            toolStripBtnZoomIn.Text = "放大";
            toolStripBtnZoomIn.Click += ToolStripBtnZoomIn_Click;
            // 
            // toolStripBtnZoomOut
            // 
            toolStripBtnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnZoomOut.Enabled = false;
            toolStripBtnZoomOut.Name = "toolStripBtnZoomOut";
            toolStripBtnZoomOut.Size = new Size(43, 24);
            toolStripBtnZoomOut.Text = "缩小";
            toolStripBtnZoomOut.Click += ToolStripBtnZoomOut_Click;
            // 
            // toolStripBtnZoomFit
            // 
            toolStripBtnZoomFit.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripBtnZoomFit.Enabled = false;
            toolStripBtnZoomFit.Name = "toolStripBtnZoomFit";
            toolStripBtnZoomFit.Size = new Size(73, 24);
            toolStripBtnZoomFit.Text = "适应窗口";
            toolStripBtnZoomFit.Click += ToolStripBtnZoomFit_Click;
            // 
            // toolStripLblZoom
            // 
            toolStripLblZoom.Name = "toolStripLblZoom";
            toolStripLblZoom.Size = new Size(49, 24);
            toolStripLblZoom.Text = "100%";
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 55);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(splitContainerLeft);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(panelViewPort);
            splitContainer.Size = new Size(1350, 563);
            splitContainer.SplitterDistance = 337;
            splitContainer.TabIndex = 2;
            // 
            // splitContainerLeft
            // 
            splitContainerLeft.Dock = DockStyle.Fill;
            splitContainerLeft.Location = new Point(0, 0);
            splitContainerLeft.Name = "splitContainerLeft";
            splitContainerLeft.Orientation = Orientation.Horizontal;
            // 
            // splitContainerLeft.Panel1
            // 
            splitContainerLeft.Panel1.Controls.Add(grpPageList);
            // 
            // splitContainerLeft.Panel2
            // 
            splitContainerLeft.Panel2.Controls.Add(grpDocInfo);
            splitContainerLeft.Size = new Size(337, 563);
            splitContainerLeft.SplitterDistance = 280;
            splitContainerLeft.TabIndex = 0;
            // 
            // grpPageList
            // 
            grpPageList.Controls.Add(listBoxPages);
            grpPageList.Dock = DockStyle.Fill;
            grpPageList.Location = new Point(0, 0);
            grpPageList.Name = "grpPageList";
            grpPageList.Size = new Size(337, 280);
            grpPageList.TabIndex = 0;
            grpPageList.TabStop = false;
            grpPageList.Text = "页面列表";
            // 
            // listBoxPages
            // 
            listBoxPages.Dock = DockStyle.Fill;
            listBoxPages.FormattingEnabled = true;
            listBoxPages.Location = new Point(3, 23);
            listBoxPages.Name = "listBoxPages";
            listBoxPages.Size = new Size(331, 254);
            listBoxPages.TabIndex = 0;
            listBoxPages.SelectedIndexChanged += ListBoxPages_SelectedIndexChanged;
            // 
            // grpDocInfo
            // 
            grpDocInfo.Controls.Add(txtDocumentInfo);
            grpDocInfo.Dock = DockStyle.Fill;
            grpDocInfo.Location = new Point(0, 0);
            grpDocInfo.Name = "grpDocInfo";
            grpDocInfo.Size = new Size(337, 279);
            grpDocInfo.TabIndex = 0;
            grpDocInfo.TabStop = false;
            grpDocInfo.Text = "文档信息";
            // 
            // txtDocumentInfo
            // 
            txtDocumentInfo.BackColor = SystemColors.Info;
            txtDocumentInfo.Dock = DockStyle.Fill;
            txtDocumentInfo.Location = new Point(3, 23);
            txtDocumentInfo.Multiline = true;
            txtDocumentInfo.Name = "txtDocumentInfo";
            txtDocumentInfo.ReadOnly = true;
            txtDocumentInfo.ScrollBars = ScrollBars.Vertical;
            txtDocumentInfo.Size = new Size(331, 253);
            txtDocumentInfo.TabIndex = 0;
            txtDocumentInfo.Text = "请打开 OFD 文档...";
            // 
            // panelViewPort
            // 
            panelViewPort.AutoScroll = true;
            panelViewPort.BackColor = Color.LightGray;
            panelViewPort.Dock = DockStyle.Fill;
            panelViewPort.Location = new Point(0, 0);
            panelViewPort.Name = "panelViewPort";
            panelViewPort.Size = new Size(1009, 563);
            panelViewPort.TabIndex = 0;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripProgressBar });
            statusStrip.Location = new Point(0, 618);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(1350, 26);
            statusStrip.TabIndex = 3;
            statusStrip.Text = "statusStrip";
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(69, 20);
            toolStripStatusLabel.Text = "准备就绪";
            // 
            // toolStripProgressBar
            // 
            toolStripProgressBar.Name = "toolStripProgressBar";
            toolStripProgressBar.Size = new Size(112, 18);
            toolStripProgressBar.Visible = false;
            // 
            // openFileDialog
            // 
            openFileDialog.DefaultExt = "ofd";
            openFileDialog.Filter = "OFD文档 (*.ofd)|*.ofd|所有文件 (*.*)|*.*";
            openFileDialog.Title = "选择要打开的OFD文档";
            // 
            // OfdViewerForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1350, 644);
            Controls.Add(splitContainer);
            Controls.Add(toolStrip);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Name = "OfdViewerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "OFD 文档查看器";
            WindowState = FormWindowState.Maximized;
            Load += OfdViewerForm_Load;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            splitContainerLeft.Panel1.ResumeLayout(false);
            splitContainerLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
            splitContainerLeft.ResumeLayout(false);
            grpPageList.ResumeLayout(false);
            grpDocInfo.ResumeLayout(false);
            grpDocInfo.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
    }
}
