using Microsoft.Extensions.Logging;
using OfdrwNet.Reader;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OfdrwNet.WinFormsDemo;

/// <summary>
/// OFD 文档查看器窗体
/// 提供 OFD 文档的查看功能，包括文档信息、页面列表和页面内容显示
/// </summary>
public partial class OfdViewerForm : Form
{
    private OfdReader _currentOfdReader;
    private OfdPageDrawer _pageDrawer;
    private List<PageInfo>? _pageList;
    private int _currentPageIndex = -1;
    private string? _currentFilePath;
    private readonly ILogger<OfdViewerForm>? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OfdViewerForm()
    {
        InitializeComponent();
        
        // 创建日志记录器
        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<OfdViewerForm>();
        }
        catch
        {
            // 忽略日志创建失败
        }
    }

    /// <summary>
    /// 带文件路径的构造函数
    /// </summary>
    /// <param name="ofdFilePath">要打开的OFD文件路径</param>
    public OfdViewerForm(string ofdFilePath) : this()
    {
        if (!string.IsNullOrEmpty(ofdFilePath) && File.Exists(ofdFilePath))
        {
            _currentFilePath = ofdFilePath;
        }
    }

    /// <summary>
    /// 窗体加载事件
    /// </summary>
    private void OfdViewerForm_Load(object sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("OFD查看器启动");
            
            // 如果指定了文件路径，自动打开
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                OpenOfdDocument(_currentFilePath);
            }
            
            UpdateUI();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "窗体加载时发生错误");
            ShowError($"窗体加载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开按钮点击事件
    /// </summary>
    private void ToolStripBtnOpen_Click(object sender, EventArgs e)
    {
        try
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenOfdDocument(openFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "打开文件对话框时发生错误");
            ShowError($"打开文件对话框失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void ToolStripBtnClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// 上一页按钮点击事件
    /// </summary>
    private void ToolStripBtnPrevPage_Click(object sender, EventArgs e)
    {
        NavigateToPage(_currentPageIndex - 1);
    }

    /// <summary>
    /// 下一页按钮点击事件
    /// </summary>
    private void ToolStripBtnNextPage_Click(object sender, EventArgs e)
    {
        NavigateToPage(_currentPageIndex + 1);
    }

    /// <summary>
    /// 页码输入框键盘事件
    /// </summary>
    private void ToolStripTxtPageNum_KeyPress(object sender, KeyPressEventArgs e)
    {
        // 只允许数字和控制字符
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
        {
            e.Handled = true;
            return;
        }

        // 处理回车键
        if (e.KeyChar == (char)Keys.Return)
        {
            e.Handled = true;
            
            if (int.TryParse(toolStripTxtPageNum.Text, out int pageNum))
            {
                NavigateToPage(pageNum - 1); // 转换为0基索引
            }
        }
    }

    /// <summary>
    /// 页面列表选择变化事件
    /// </summary>
    private void ListBoxPages_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (listBoxPages.SelectedIndex >= 0 && listBoxPages.SelectedIndex != _currentPageIndex)
        {
            NavigateToPage(listBoxPages.SelectedIndex);
        }
    }

    /// <summary>
    /// 打开 OFD 文档
    /// </summary>
    /// <param name="filePath">文件路径</param>
    private async void OpenOfdDocument(string filePath)
    {
        try
        {
            toolStripStatusLabel.Text = "正在加载文档...";
            Application.DoEvents();

            // 关闭之前的文档
            CloseCurrentDocument();

            _logger?.LogInformation("开始打开OFD文档: {FilePath}", filePath);

            // 预先验证文件
            var preValidationResult = PreValidateOfdFile(filePath);
            if (!preValidationResult.IsValid)
            {
                throw new InvalidOperationException($"文件验证失败: {preValidationResult.ErrorMessage}");
            }

            // 在后台线程中打开文档
            await Task.Run(() =>
            {
                try
                {
                    _currentOfdReader = new OfdReader(filePath);
                    _pageDrawer = new OfdPageDrawer(_currentOfdReader);
                    _pageList = _currentOfdReader.GetPageList();
                }
                catch (Exception ex)
                {
                    // 提供更具体的错误信息
                    if (ex.Message.Contains("End of Central Directory") || 
                        ex.Message.Contains("Central Directory"))
                    {
                        throw new InvalidOperationException(
                            "OFD文件的ZIP压缩包结构损坏。\n" +
                            "可能的原因：\n" +
                            "1. 文件在下载或传输过程中损坏\n" +
                            "2. 文件被病毒软件修改\n" +
                            "3. 磁盘存储错误\n" +
                            "4. 文件正在被其他程序使用\n\n" +
                            "建议：\n" +
                            "- 重新下载或获取文件\n" +
                            "- 检查文件是否被其他程序占用\n" +
                            "- 尝试从备份恢复文件", ex);
                    }
                    else if (ex.Message.Contains("解压") || ex.Message.Contains("ZIP"))
                    {
                        throw new InvalidOperationException(
                            $"无法解压OFD文件: {ex.Message}\n\n" +
                            "这通常表示文件已损坏或不是有效的OFD格式。", ex);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"打开OFD文档时发生未知错误: {ex.Message}\n\n" +
                            "请检查文件是否为有效的OFD格式。", ex);
                    }
                }
            });

            _currentFilePath = filePath;
            
            // 更新文档信息
            UpdateDocumentInfo();
            
            // 更新页面列表
            UpdatePageList();
            
            // 显示第一页
            if (_pageList?.Count > 0)
            {
                NavigateToPage(0);
            }

            toolStripStatusLabel.Text = $"文档加载完成 - {Path.GetFileName(filePath)}";
            _logger?.LogInformation("OFD文档打开成功: {FilePath}", filePath);
        }
        catch (InvalidOperationException)
        {
            // 重新抛出已包装的异常
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "打开OFD文档失败: {FilePath}", filePath);
            
            // 根据异常类型提供更具体的错误信息
            string errorMessage = ex.Message;
            if (ex is UnauthorizedAccessException)
            {
                errorMessage = "没有访问文件的权限。请检查文件是否被其他程序占用或者您是否有足够的权限。";
            }
            else if (ex is FileNotFoundException)
            {
                errorMessage = "文件不存在或已被移动。";
            }
            else if (ex is IOException)
            {
                errorMessage = $"文件读取错误: {ex.Message}";
            }
            
            ShowError($"打开文档失败: {errorMessage}");
            toolStripStatusLabel.Text = "文档加载失败";
        }
        finally
        {
            UpdateUI();
        }
    }

    /// <summary>
    /// 预验证OFD文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>验证结果</returns>
    private (bool IsValid, string ErrorMessage) PreValidateOfdFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // 检查文件大小
            if (fileInfo.Length == 0)
            {
                return (false, "文件为空");
            }
            
            if (fileInfo.Length < 22)
            {
                return (false, "文件太小，不是有效的OFD文件");
            }
            
            // 检查文件是否被占用
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // 检查ZIP文件头
                    var buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    
                    if (buffer[0] != 0x50 || buffer[1] != 0x4B)
                    {
                        return (false, "文件不是有效的ZIP格式（OFD文件必须是ZIP压缩包）");
                    }
                }
            }
            catch (IOException ex)
            {
                return (false, $"无法访问文件: {ex.Message}");
            }
            
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"文件验证时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭当前文档
    /// </summary>
    private void CloseCurrentDocument()
    {
        try
        {
            _currentOfdReader?.Dispose();
            _currentOfdReader = null;
            _pageDrawer?.Dispose();
            _pageDrawer = null;
            _pageList = null;
            _currentPageIndex = -1;
            _currentFilePath = null;

            // 清空界面
            txtDocumentInfo.Text = "请打开 OFD 文档...";
            txtPageContent.Text = "请选择页面查看内容...";
            listBoxPages.Items.Clear();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "关闭当前文档时发生错误");
        }
    }

    /// <summary>
    /// 更新文档信息显示
    /// </summary>
    private void UpdateDocumentInfo()
    {
        if (_currentOfdReader == null || string.IsNullOrEmpty(_currentFilePath))
        {
            txtDocumentInfo.Text = "请打开 OFD 文档...";
            return;
        }

        try
        {
            var fileInfo = new FileInfo(_currentFilePath);
            var pageCount = _pageList?.Count ?? 0;

            var sb = new StringBuilder();
            sb.AppendLine("=== 文档信息 ===");
            sb.AppendLine($"文件名: {fileInfo.Name}");
            sb.AppendLine($"文件大小: {FormatFileSize(fileInfo.Length)}");
            sb.AppendLine($"修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"页面总数: {pageCount}");
            sb.AppendLine();

            var docInfo = _currentOfdReader.GetDocumentInfo();
            if (docInfo != null)
            {
                sb.AppendLine($"  DocID: {docInfo.DocId}");
                sb.AppendLine($"  标题: {docInfo.Title}");
                sb.AppendLine($"  作者: {docInfo.Author}");
                sb.AppendLine($"  主题: {docInfo.Subject}");
                sb.AppendLine($"  关键字: {docInfo.Keywords}");
                sb.AppendLine($"  创建日期: {docInfo.CreationDate}");
                sb.AppendLine($"  修改日期: {docInfo.ModificationDate}");
                sb.AppendLine();
            }

            // 添加文本统计信息
            try
            {
                var extractor = new ContentExtractor(_currentOfdReader);
                var stats = extractor.GetDocumentStatistics();
                
                sb.AppendLine("=== 文本统计 ===");
                sb.AppendLine($"含文本页面: {stats.PagesWithText} / {stats.TotalPages} 页");
                sb.AppendLine($"文本对象数: {stats.TotalTextObjects}");
                sb.AppendLine($"总字符数: {stats.TotalCharacters:N0}");
                sb.AppendLine();
            }
            catch (Exception textStatsEx)
            {
                sb.AppendLine("=== 文本统计 ===");
                sb.AppendLine($"获取文本统计失败: {textStatsEx.Message}");
                sb.AppendLine();
            }
            
            sb.AppendLine("=== 技术信息 ===");
            sb.AppendLine($"工作目录: {_currentOfdReader.GetWorkDir()}");
            
            // 添加页面大小信息
            if (_pageList != null && _pageList.Count > 0)
            {
                var firstPage = _pageList[0];
                sb.AppendLine($"首页大小: {firstPage.Size}");
                sb.AppendLine($"首页ID: {firstPage.Id}");
            }

            txtDocumentInfo.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            txtDocumentInfo.Text = $"获取文档信息失败: {ex.Message}";
            _logger?.LogError(ex, "更新文档信息时发生错误");
        }
    }

    private void UpdatePageList()
    {
        listBoxPages.Items.Clear();

        if (_pageList == null || _pageList.Count == 0 || _currentOfdReader == null)
        {
            return;
        }

        try
        {
            var extractor = new ContentExtractor(_currentOfdReader);
            
            for (int i = 0; i < _pageList.Count; i++)
            {
                var page = _pageList[i];
                
                // 获取页面文本摘要
                string textSummary = "";
                try
                {
                    var summary = extractor.GetPageTextSummary(i + 1, 50);
                    textSummary = summary.Replace("\r\n", " ").Replace("\n", " ");
                    
                    if (textSummary.Length > 50)
                    {
                        textSummary = textSummary.Substring(0, 47) + "...";
                    }
                    
                    if (!string.IsNullOrWhiteSpace(textSummary) && textSummary != "（此页面无文本内容）")
                    {
                        textSummary = " - " + textSummary;
                    }
                    else
                    {
                        textSummary = " - (无文本)";
                    }
                }
                catch
                {
                    textSummary = " - (提取失败)";
                }
                
                var pageText = $"第 {i + 1} 页 (ID: {page.Id}){textSummary}";
                listBoxPages.Items.Add(pageText);
            }
        }
        catch (Exception ex)
        {
            // 如果增强显示失败，回退到基本显示
            try
            {
                for (int i = 0; i < _pageList.Count; i++)
                {
                    var page = _pageList[i];
                    var pageText = $"第 {i + 1} 页 (ID: {page.Id})";
                    listBoxPages.Items.Add(pageText);
                }
            }
            catch
            {
                // 如果连基本显示都失败了
            }
            
            _logger?.LogError(ex, "更新页面列表时发生错误");
            ShowError($"更新页面列表失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    /// <param name="pageIndex">页面索引（0基）</param>
    private void NavigateToPage(int pageIndex)
    {
        if (_pageList == null || pageIndex < 0 || pageIndex >= _pageList.Count)
        {
            return;
        }

        try
        {
            _currentPageIndex = pageIndex;
            var pageInfo = _pageList[pageIndex];

            // 更新页面内容显示
            UpdatePageContent(pageInfo);

            // 更新页面列表选择
            if (listBoxPages.SelectedIndex != pageIndex)
            {
                listBoxPages.SelectedIndex = pageIndex;
            }

            // 更新工具栏
            toolStripTxtPageNum.Text = (pageIndex + 1).ToString();
            
            UpdateUI();
            
            toolStripStatusLabel.Text = $"第 {pageIndex + 1} 页 / 共 {_pageList.Count} 页";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "导航到页面 {PageIndex} 时发生错误", pageIndex);
            ShowError($"显示页面失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新页面内容显示
    /// </summary>
    /// <param name="pageInfo">页面信息</param>
    private void UpdatePageContent(PageInfo pageInfo)
    {
        try
        {
            var content = new StringBuilder();
            content.AppendLine($"=== 第 {pageInfo.Index} 页信息 ===");
            content.AppendLine($"页面ID: {pageInfo.Id}");
            content.AppendLine($"页面大小: {pageInfo.Size}");
            content.AppendLine($"页面路径: {pageInfo.PageAbsLoc}");
            content.AppendLine($"模板数量: {pageInfo.Templates.Count}");
            content.AppendLine();

            // 尝试使用OfdPageDrawer渲染页面
            if (_currentOfdReader != null)
            {
                try
                {
                    // 初始化页面绘制器
                    if (_pageDrawer == null)
                    {
                        _pageDrawer = new OfdPageDrawer(_currentOfdReader);
                    }

                    // 绘制页面到图片
                    var pageImage = _pageDrawer.DrawPageToBitmap(pageInfo.Index, 800, 600);
                    
                    content.AppendLine("=== 页面渲染 ===");
                    content.AppendLine($"页面已成功渲染为 {pageImage.Width}x{pageImage.Height} 图像");
                    content.AppendLine("渲染后的页面图像可以在实际的图像查看器中显示");
                    content.AppendLine($"图像格式: {pageImage.PixelFormat}");
                    content.AppendLine();

                    // 将图像保存到临时文件以便调试
                    var tempPath = Path.Combine(Path.GetTempPath(), $"ofd_page_{pageInfo.Index}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    pageImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                    content.AppendLine($"调试信息: 页面图像已保存到 {tempPath}");
                    content.AppendLine();

                    pageImage.Dispose();
                }
                catch (Exception renderEx)
                {
                    content.AppendLine("=== 页面渲染 ===");
                    content.AppendLine($"页面渲染失败: {renderEx.Message}");
                    content.AppendLine($"错误详情: {renderEx.StackTrace}");
                    content.AppendLine();
                }

                // 添加文本内容提取
                try
                {
                    var extractor = new ContentExtractor(_currentOfdReader);
                    var pageTexts = extractor.GetPageContent(pageInfo.Index);
                    
                    content.AppendLine("=== 页面文本内容 ===");
                    if (pageTexts.Count > 0)
                    {
                        content.AppendLine($"文本对象数量: {pageTexts.Count}");
                        content.AppendLine($"总字符数: {pageTexts.Sum(t => t.Length)}");
                        content.AppendLine();
                        
                        content.AppendLine("文本内容:");
                        for (int i = 0; i < pageTexts.Count; i++)
                        {
                            content.AppendLine($"{i + 1}. {pageTexts[i]}");
                        }
                    }
                    else
                    {
                        content.AppendLine("此页面无文本内容");
                    }
                    content.AppendLine();
                }
                catch (Exception textEx)
                {
                    content.AppendLine("=== 页面文本内容 ===");
                    content.AppendLine($"提取文本时发生错误: {textEx.Message}");
                    content.AppendLine();
                }
            }
            else
            {
                content.AppendLine("=== 页面文本内容 ===");
                content.AppendLine("OFD阅读器未初始化");
                content.AppendLine();
            }

            // 添加模板信息
            if (pageInfo.Templates.Count > 0)
            {
                content.AppendLine("=== 模板信息 ===");
                for (int i = 0; i < pageInfo.Templates.Count; i++)
                {
                    var template = pageInfo.Templates[i];
                    content.AppendLine($"模板 {i + 1}:");
                    content.AppendLine($"  ID: {template.Id}");
                    content.AppendLine($"  名称: {template.TemplatePageName}");
                    content.AppendLine($"  层级: {template.Order}");
                    content.AppendLine();
                }
            }

            content.AppendLine("=== 页面 XML 结构 ===");
            if (pageInfo.Obj != null)
            {
                content.AppendLine(FormatXml(pageInfo.Obj.ToString()));
            }
            else
            {
                content.AppendLine("页面对象为空");
            }

            txtPageContent.Text = content.ToString();
        }
        catch (Exception ex)
        {
            txtPageContent.Text = $"显示页面内容时发生错误: {ex.Message}";
            _logger?.LogError(ex, "更新页面内容时发生错误");
        }
    }

    /// <summary>
    /// 更新用户界面状态
    /// </summary>
    private void UpdateUI()
    {
        bool hasDocument = _currentOfdReader != null && _pageList != null;
        bool hasPages = hasDocument && _pageList!.Count > 0;
        bool hasCurrentPage = hasPages && _currentPageIndex >= 0;

        // 更新工具栏按钮状态
        toolStripBtnPrevPage.Enabled = hasCurrentPage && _currentPageIndex > 0;
        toolStripBtnNextPage.Enabled = hasCurrentPage && _currentPageIndex < _pageList!.Count - 1;
        toolStripTxtPageNum.Enabled = hasPages;

        // 更新页码显示
        if (hasPages)
        {
            toolStripLblPageTotal.Text = $"/ {_pageList!.Count}";
            if (!hasCurrentPage)
            {
                toolStripTxtPageNum.Text = "";
            }
        }
        else
        {
            toolStripLblPageTotal.Text = "/ 0";
            toolStripTxtPageNum.Text = "";
        }

        // 更新窗体标题
        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            this.Text = $"OFD 文档查看器 - {Path.GetFileName(_currentFilePath)}";
        }
        else
        {
            this.Text = "OFD 文档查看器";
        }
    }

    /// <summary>
    /// 格式化 XML 字符串
    /// </summary>
    /// <param name="xml">XML 字符串</param>
    /// <returns>格式化后的 XML</returns>
    private static string FormatXml(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString();
        }
        catch
        {
            return xml; // 如果解析失败，返回原始字符串
        }
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化后的文件大小字符串</returns>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    /// <param name="message">错误消息</param>
    private void ShowError(string message)
    {
        MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// 窗体关闭时的清理工作
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            CloseCurrentDocument();
            _logger?.LogInformation("OFD查看器关闭");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "关闭窗体时发生错误");
        }

        base.OnFormClosing(e);
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using (var openFileDialog = new OpenFileDialog())
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(openFileDialog.FileName);
            }
        }
    }

    private void OpenFile(string filePath)
    {
        try
        {
            _currentOfdReader?.Dispose();
            _pageDrawer?.Dispose();

            _currentOfdReader = new OfdReader(filePath);
            _pageDrawer = new OfdPageDrawer(_currentOfdReader);

            this.Text = $"OFD Viewer - {Path.GetFileName(filePath)}";
            UpdatePageList();
            UpdateDocumentInfo();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }



    private void lvPages_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lvPages.SelectedItems.Count > 0)
        {
            var selectedItem = lvPages.SelectedItems[0];
            if (selectedItem.Tag is int pageNum)
            {
                DisplayPage(pageNum);
            }
        }
    }

    private void DisplayPage(int pageNum)
    {
        if (_currentOfdReader == null || _pageDrawer == null) return;

        try
        {
            var pageInfo = _currentOfdReader.GetPage(pageNum);
            if (pageInfo == null)
            {
                MessageBox.Show($"无法加载页面 {pageNum}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 显示页面XML内容
            txtPageContent.Text = pageInfo.Source.ToString();

            // 绘制页面
            var physicalBox = pageInfo.GetPhysicalBox();
            var bitmap = new Bitmap((int)physicalBox.Width, (int)physicalBox.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                _pageDrawer.Draw(g, pageNum);
            }
            pbPagePreview.Image = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"显示页面 {pageNum} 时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OfdViewerForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        _currentOfdReader?.Dispose();
        _pageDrawer?.Dispose();
    }
}