// Clean single authoritative implementation of OfdViewerForm.
// Removed duplicated legacy code and PictureBox rendering paths.

using Microsoft.Extensions.Logging;
using OfdrwNet.Reader;
using OfdrwNet.Reader.Model;
using OfdrwNet.Reader.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfdrwNet.WinFormsDemo;

public partial class OfdViewerForm : Form
{
    private string? _currentFilePath;
    private List<PageInfo>? _pageList;
    private int _currentPageIndex = -1;
    private readonly ILogger<OfdViewerForm>? _logger;
    private float _zoomFactor = 1.0f;
    private OfdReader? _ofdReader;
    private RenderingEngine? _renderingEngine;
    private bool _debugMode;
    private Viewer.PageViewportControl? _viewport;

    public OfdViewerForm()
    {
        InitializeComponent();
        KeyPreview = true;
        KeyDown += OfdViewerForm_KeyDown;
        Load += OfdViewerForm_Load;
        try { using var lf = LoggerFactory.Create(b => b.AddConsole()); _logger = lf.CreateLogger<OfdViewerForm>(); } catch { }
    }

    public OfdViewerForm(string ofdFilePath) : this()
    { if (File.Exists(ofdFilePath)) _currentFilePath = ofdFilePath; }

    private void OfdViewerForm_Load(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentFilePath)) OpenOfdDocument(_currentFilePath);
        UpdateUI();
    }

    #region 打开/关闭
    private async void OpenOfdDocument(string filePath)
    {
        try
        {
            toolStripStatusLabel.Text = "正在加载文档...";
            toolStripProgressBar.Visible = true;
            Application.DoEvents();
            CloseCurrentDocument();
            var validate = ValidateOfdFile(filePath);
            if (!validate.IsValid) throw new InvalidOperationException($"文件验证失败: {validate.ErrorMessage}");
            await Task.Run(() =>
            {
                _ofdReader = new OfdReader(filePath);
                var rm = _ofdReader.GetResourceManager();
                _renderingEngine = new RenderingEngine(rm);
                _pageList = _ofdReader.GetPageList();
            });
            _currentFilePath = filePath;
            UpdateDocumentInfo();
            UpdatePageList();
            if (_pageList?.Count > 0) NavigateToPage(0);
            toolStripStatusLabel.Text = $"加载完成 - {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        { _logger?.LogError(ex, "打开OFD失败"); ShowError($"打开失败: {ex.Message}"); toolStripStatusLabel.Text = "文档加载失败"; }
        finally { toolStripProgressBar.Visible = false; UpdateUI(); }
    }

    private (bool IsValid, string ErrorMessage) ValidateOfdFile(string filePath)
    {
        try
        {
            var fi = new FileInfo(filePath);
            if (fi.Length == 0) return (false, "文件为空");
            if (fi.Length < 22) return (false, "文件过小");
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> head = stackalloc byte[4]; fs.Read(head);
            if (head[0] != 0x50 || head[1] != 0x4B) return (false, "非ZIP格式");
            return (true, string.Empty);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private void CloseCurrentDocument()
    {
        try
        {
            _pageList = null; _currentPageIndex = -1; _currentFilePath = null; _zoomFactor = 1f;
            _ofdReader?.Dispose(); _ofdReader = null;
            _renderingEngine?.Dispose(); _renderingEngine = null;
            txtDocumentInfo.Text = "请打开 OFD 文档...";
            listBoxPages.Items.Clear();
            _viewport?.SetPageContent(null, null);
            _viewport?.Invalidate();
        }
        catch (Exception ex) { _logger?.LogError(ex, "关闭文档失败"); }
    }
    #endregion

    #region 文档信息
    private async void UpdateDocumentInfo()
    {
        if (_pageList == null || _ofdReader == null || string.IsNullOrEmpty(_currentFilePath)) { txtDocumentInfo.Text = "请打开 OFD 文档..."; return; }
        try
        {
            var fi = new FileInfo(_currentFilePath);
            var metaTask = _ofdReader.GetDocumentInfoAsync();
            var valTask = _ofdReader.ValidateDocumentAsync();
            await Task.WhenAll(metaTask, valTask);
            var meta = metaTask.Result; var val = valTask.Result;
            var sb = new StringBuilder();
            sb.AppendLine("=== 文档信息 ===");
            sb.AppendLine($"文件: {fi.Name}");
            sb.AppendLine($"大小: {FormatFileSize(fi.Length)}");
            sb.AppendLine($"修改: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"页面: {_pageList.Count}");
            sb.AppendLine();
            sb.AppendLine("=== 属性 ===");
            sb.AppendLine($"标题: {meta?.Title ?? "(无)"}");
            sb.AppendLine($"作者: {meta?.Author ?? "(无)"}");
            if (meta?.CreationDate != null) sb.AppendLine($"创建: {meta.CreationDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"版本: {meta?.Version.ToString() ?? "Unknown"}");
            sb.AppendLine();
            sb.AppendLine("=== 验证 ===");
            sb.AppendLine($"状态: {(val.IsValid ? "✅ 通过" : "❌ 失败")}");
            if (val.Errors?.Any() == true) sb.AppendLine($"错误: {val.Errors.Count}");
            if (val.Warnings?.Any() == true) sb.AppendLine($"警告: {val.Warnings.Count}");
            sb.AppendLine();
            sb.AppendLine("=== 页面尺寸统计 ===");
            foreach (var kv in _pageList.GroupBy(p => $"{p.Width:F1}mm x {p.Height:F1}mm"))
                sb.AppendLine($"  {kv.Key}: {kv.Count()} 页");
            txtDocumentInfo.Text = sb.ToString();
        }
        catch (Exception ex) { txtDocumentInfo.Text = $"文档信息获取失败: {ex.Message}"; _logger?.LogError(ex, "获取文档信息失败"); }
    }

    private void UpdatePageList()
    {
        listBoxPages.Items.Clear();
        if (_pageList == null) return;
        for (int i = 0; i < _pageList.Count; i++) listBoxPages.Items.Add($"第 {i + 1} 页 (ID:{_pageList[i].Id})");
    }
    #endregion

    #region UI 状态
    private void UpdateUI()
    {
        bool hasPages = _pageList != null && _pageList.Count > 0;
        bool hasCurrent = hasPages && _currentPageIndex >= 0;
        toolStripBtnPrevPage.Enabled = hasCurrent && _currentPageIndex > 0;
        toolStripBtnNextPage.Enabled = hasCurrent && _currentPageIndex < (_pageList?.Count ?? 0) - 1;
        toolStripTxtPageNum.Enabled = hasPages;
        toolStripBtnZoomIn.Enabled = toolStripBtnZoomOut.Enabled = toolStripBtnZoomFit.Enabled = hasCurrent;
        toolStripLblPageTotal.Text = hasPages ? $"/ {_pageList!.Count}" : "/ 0";
        toolStripTxtPageNum.Text = hasCurrent ? (_currentPageIndex + 1).ToString() : "";
        toolStripLblZoom.Text = $"{_zoomFactor * 100:F0}%";
        Text = string.IsNullOrEmpty(_currentFilePath) ? "OFD 文档查看器" : $"OFD 文档查看器 - {Path.GetFileName(_currentFilePath)}";
    }
    #endregion

    #region 导航/渲染
    private void NavigateToPage(int pageIndex)
    {
        if (_pageList == null || pageIndex < 0 || pageIndex >= _pageList.Count) return;
        _currentPageIndex = pageIndex;
        if (listBoxPages.SelectedIndex != pageIndex) listBoxPages.SelectedIndex = pageIndex;
        RenderCurrentPage();
        UpdateUI();
        toolStripStatusLabel.Text = $"第 {pageIndex + 1} 页 / 共 {_pageList.Count} 页";
    }

    private void RenderCurrentPage()
    {
        if (_pageList == null || _ofdReader == null || _currentPageIndex < 0) return;
        EnsureViewport();
        if (_viewport == null) return;
        try
        {
            var rdPage = _pageList[_currentPageIndex];
            double dpi = 96.0;
            double baseW = rdPage.Width / 25.4 * dpi;
            double baseH = rdPage.Height / 25.4 * dpi;
            if (baseW <= 0) baseW = 800; if (baseH <= 0) baseH = 1000;
            int w = (int)(baseW * _zoomFactor);
            int h = (int)(baseH * _zoomFactor);
            _viewport.Zoom = _zoomFactor;
            _viewport.IsLoading = true; _viewport.Invalidate();
            if (_renderingEngine != null)
            {
                try
                {
                    var rc = RenderContext.CreateHighQuality();
                    rc.UpdateDpi((int)dpi, (int)dpi);
                    rc.ViewPort = new Rectangle(0, 0, w, h);

                    System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 页面内容对象数量: {rdPage.ContentObjects.Count}");

                    if (rdPage.ContentObjects.Count == 0)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 页面内容为空，尝试提取渲染对象");
                        double sx = w / (rdPage.Width <= 0 ? 1 : rdPage.Width);
                        double sy = h / (rdPage.Height <= 0 ? 1 : rdPage.Height);
                        System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 页面缩放比例: sx={sx}, sy={sy}");

                        var ros = PageContentExtractor.ExtractRenderObjects(rdPage, sx, sy);
                        System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 提取到 {ros?.Count() ?? 0} 个渲染对象");

                        if (ros != null)
                        {
                            foreach (var ro in ros.OfType<ContentObject>())
                            {
                                rdPage.ContentObjects.Add(ro);
                                System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 添加渲染对象: {ro.GetType().Name} (ID: {ro.ResourceId})");
                            }
                        }

                        System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 最终页面内容对象数量: {rdPage.ContentObjects.Count}");
                    }

                    _viewport.SetRenderContext(rc);

                    var renderObjects = rdPage.ContentObjects.Cast<RenderObject>().ToList();
                    System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 设置页面内容，渲染对象数量: {renderObjects.Count}");

                    foreach (var obj in renderObjects.Take(5)) // 只显示前5个对象避免日志过多
                    {
                        System.Diagnostics.Trace.WriteLine($"[OfdViewerForm] 渲染对象: {obj.GetType().Name} (ID: {obj.Id}, 可见: {obj.Visible})");
                    }

                    _viewport.SetPageContent(renderObjects, _renderingEngine);
                }
                catch (Exception rex)
                { _logger?.LogWarning(rex, "页面渲染失败，空视口"); _viewport.SetPageContent(null, _renderingEngine); }
            }
            else _viewport.SetPageContent(null, null);
            _viewport.IsLoading = false; _viewport.Size = new Size(w, h); _viewport.Invalidate();
            CenterViewport();
        }
        catch (Exception ex) { _logger?.LogError(ex, "渲染页面异常"); ShowError($"渲染失败: {ex.Message}"); }
    }

    private void CenterViewport()
    {
        if (_viewport == null) return;
        int x = Math.Max(0, (panelViewPort.ClientSize.Width - _viewport.Width) / 2);
        int y = Math.Max(0, (panelViewPort.ClientSize.Height - _viewport.Height) / 2);
        _viewport.Location = new Point(x, y);
    }

    private void EnsureViewport()
    {
        if (_viewport != null && !_viewport.IsDisposed) return;
        _viewport = new Viewer.PageViewportControl
        { BackColor = Color.White, Location = new Point(0, 0), Size = new Size(800, 1000) };
        panelViewPort.Controls.Add(_viewport);
        _viewport.BringToFront();
    }
    #endregion

    #region 缩放
    private void ZoomPage(float factor)
    { _zoomFactor = Math.Clamp(_zoomFactor * factor, 0.1f, 5f); RenderCurrentPage(); UpdateUI(); }

    private void FitToWindow()
    {
        if (_pageList == null || _currentPageIndex < 0) return;
        var rdPage = _pageList[_currentPageIndex];
        double dpi = 96.0;
        double baseW = rdPage.Width / 25.4 * dpi;
        double baseH = rdPage.Height / 25.4 * dpi;
        if (baseW <= 0) baseW = 800; if (baseH <= 0) baseH = 1000;
        float wr = (float)(panelViewPort.ClientSize.Width - 40) / (float)baseW;
        float hr = (float)(panelViewPort.ClientSize.Height - 40) / (float)baseH;
        _zoomFactor = Math.Clamp(Math.Min(wr, hr), 0.1f, 5f);
        RenderCurrentPage(); UpdateUI();
    }

    private static string FormatFileSize(long bytes)
    { string[] suf = { "B", "KB", "MB", "GB", "TB" }; int i = 0; decimal num = bytes; while (Math.Round(num / 1024) >= 1) { num /= 1024; i++; } return $"{num:n1} {suf[i]}"; }
    private void ShowError(string msg) => MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    #endregion

    #region 生命周期/快捷键
    protected override void OnFormClosing(FormClosingEventArgs e)
    { try { CloseCurrentDocument(); } catch { } base.OnFormClosing(e); }

    private void OfdViewerForm_KeyDown(object? sender, KeyEventArgs e)
    { if (e.KeyCode == Keys.F9) { _debugMode = !_debugMode; toolStripStatusLabel.Text = _debugMode ? "调试模式: 开" : "调试模式: 关"; RenderCurrentPage(); } }
    #endregion

    #region Designer 事件转发
    private void toolStripTxtPageNum_KeyPress(object sender, KeyPressEventArgs e) => ToolStripTxtPageNum_KeyPress(sender, e);
    private void listBoxPages_SelectedIndexChanged(object sender, EventArgs e) => ListBoxPages_SelectedIndexChanged(sender, e);
    private void toolStripBtnOpen_Click(object sender, EventArgs e) => ToolStripBtnOpen_Click(sender, e);
    private void toolStripBtnPrevPage_Click(object sender, EventArgs e) => ToolStripBtnPrevPage_Click(sender, e);
    private void toolStripBtnNextPage_Click(object sender, EventArgs e) => ToolStripBtnNextPage_Click(sender, e);
    private void toolStripBtnZoomIn_Click(object sender, EventArgs e) => ToolStripBtnZoomIn_Click(sender, e);
    private void toolStripBtnZoomOut_Click(object sender, EventArgs e) => ToolStripBtnZoomOut_Click(sender, e);
    private void toolStripBtnZoomFit_Click(object sender, EventArgs e) => ToolStripBtnZoomFit_Click(sender, e);
    #endregion

    #region 工具栏事件实现
    private void ToolStripBtnOpen_Click(object? sender, EventArgs e)
    { if (openFileDialog.ShowDialog() == DialogResult.OK) OpenOfdDocument(openFileDialog.FileName); }
    private void ToolStripBtnPrevPage_Click(object? sender, EventArgs e) => NavigateToPage(_currentPageIndex - 1);
    private void ToolStripBtnNextPage_Click(object? sender, EventArgs e) => NavigateToPage(_currentPageIndex + 1);
    private void ToolStripBtnZoomIn_Click(object? sender, EventArgs e) => ZoomPage(1.25f);
    private void ToolStripBtnZoomOut_Click(object? sender, EventArgs e) => ZoomPage(0.8f);
    private void ToolStripBtnZoomFit_Click(object? sender, EventArgs e) => FitToWindow();
    private void ListBoxPages_SelectedIndexChanged(object? sender, EventArgs e) { if (listBoxPages.SelectedIndex >= 0 && listBoxPages.SelectedIndex != _currentPageIndex) NavigateToPage(listBoxPages.SelectedIndex); }
    private void ToolStripTxtPageNum_KeyPress(object? sender, KeyPressEventArgs e)
    { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) { e.Handled = true; return; } if (e.KeyChar == (char)Keys.Return) { e.Handled = true; if (int.TryParse(toolStripTxtPageNum.Text, out int p)) NavigateToPage(p - 1); } }
    #endregion

    #region 菜单事件桥接
    // 菜单项事件 -> 复用工具栏逻辑，避免重复实现
    private void OpenToolStripMenuItem_Click(object? sender, EventArgs e) => ToolStripBtnOpen_Click(sender, e);
    private void ExitToolStripMenuItem_Click(object? sender, EventArgs e) => Close();
    private void ZoomInToolStripMenuItem_Click(object? sender, EventArgs e) => ToolStripBtnZoomIn_Click(sender, e);
    private void ZoomOutToolStripMenuItem_Click(object? sender, EventArgs e) => ToolStripBtnZoomOut_Click(sender, e);
    private void ZoomFitToolStripMenuItem_Click(object? sender, EventArgs e) => ToolStripBtnZoomFit_Click(sender, e);
    #endregion
}
