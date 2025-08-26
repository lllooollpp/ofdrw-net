using Microsoft.Extensions.Logging;
using OfdrwNet.WinFormsDemo.Converters;
using System.IO.Compression;

namespace OfdrwNet.WinFormsDemo;

/// <summary>
/// 主窗体 - OFDRW.NET文档转换工具
/// </summary>
public partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private Word2OfdConverter? _wordConverter;
    private Html2OfdConverter? _htmlConverter;
    private Pdf2OfdConverter? _pdfConverter;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isConverting = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
        
        // 创建简单的日志记录器
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MainForm>();
        
        InitializeConverters();
        InitializeUI();
    }

    /// <summary>
    /// 初始化转换器
    /// </summary>
    private void InitializeConverters()
    {
        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            
            _wordConverter = new Word2OfdConverter(loggerFactory.CreateLogger<Word2OfdConverter>());
            _wordConverter.ProgressChanged += OnConverterProgressChanged;
            _wordConverter.ConversionCompleted += OnConverterCompleted;

            _htmlConverter = new Html2OfdConverter(loggerFactory.CreateLogger<Html2OfdConverter>());
            _htmlConverter.ProgressChanged += OnConverterProgressChanged;
            _htmlConverter.ConversionCompleted += OnConverterCompleted;

            _pdfConverter = new Pdf2OfdConverter(loggerFactory.CreateLogger<Pdf2OfdConverter>());
            _pdfConverter.ProgressChanged += OnConverterProgressChanged;
            _pdfConverter.ConversionCompleted += OnConverterCompleted;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化转换器时发生错误: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置文件对话框过滤器
        openFileDialog.Filter = FileTypeDetector.GetFileDialogFilter();
        openFileDialog.Multiselect = false;
        
        // 设置保存对话框
        saveFileDialog.Filter = "OFD文件 (*.ofd)|*.ofd|所有文件 (*.*)|*.*";
        saveFileDialog.DefaultExt = "ofd";
        
        // 更新状态
        UpdateStatus("准备就绪");
        UpdateFileInfo("请选择要转换的文件...");
    }

    /// <summary>
    /// 窗体加载事件
    /// </summary>
    private void MainForm_Load(object sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("OFDRW.NET文档转换工具启动");
            
            // 设置窗体属性
            this.Text = $"OFDRW.NET 文档转换工具 v1.0 - .NET {Environment.Version}";
            
            // 检查运行环境
            CheckRuntimeEnvironment();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "窗体加载时发生错误");
            MessageBox.Show($"应用程序启动时发生错误: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 检查运行环境
    /// </summary>
    private void CheckRuntimeEnvironment()
    {
        var runtimeInfo = $".NET版本: {Environment.Version}\n" +
                         $"操作系统: {Environment.OSVersion}\n" +
                         $"处理器架构: {Environment.ProcessorCount} cores";
        
        _logger.LogInformation("运行环境信息: {RuntimeInfo}", runtimeInfo);
    }

    /// <summary>
    /// 浏览输入文件按钮点击事件
    /// </summary>
    private void BtnBrowseInput_Click(object sender, EventArgs e)
    {
        try
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFile = openFileDialog.FileName;
                txtInputFile.Text = selectedFile;
                
                // 自动检测转换类型
                AutoDetectConversionType(selectedFile);
                
                // 自动生成输出文件名
                AutoGenerateOutputFileName(selectedFile);
                
                // 显示文件信息
                DisplayFileInfo(selectedFile);
                
                _logger.LogInformation("选择输入文件: {FileName}", selectedFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "浏览输入文件时发生错误");
            MessageBox.Show($"选择文件时发生错误: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 浏览输出文件按钮点击事件
    /// </summary>
    private void BtnBrowseOutput_Click(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(txtInputFile.Text))
            {
                var inputFileName = Path.GetFileNameWithoutExtension(txtInputFile.Text);
                saveFileDialog.FileName = $"{inputFileName}.ofd";
            }
            
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputFile.Text = saveFileDialog.FileName;
                _logger.LogInformation("选择输出文件: {FileName}", saveFileDialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "浏览输出文件时发生错误");
            MessageBox.Show($"选择输出文件时发生错误: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 开始转换按钮点击事件
    /// </summary>
    private async void BtnConvert_Click(object sender, EventArgs e)
    {
        if (_isConverting)
            return;

        try
        {
            // 验证输入
            if (!ValidateInput())
                return;

            // 开始转换
            await StartConversion();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换过程中发生错误");
            MessageBox.Show($"转换失败: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            ResetConversionUI();
        }
    }

    /// <summary>
    /// 取消转换按钮点击事件
    /// </summary>
    private void BtnCancel_Click(object sender, EventArgs e)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            UpdateStatus("正在取消转换...");
            _logger.LogInformation("用户取消转换操作");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消转换时发生错误");
        }
    }

    /// <summary>
    /// 清空按钮点击事件
    /// </summary>
    private void BtnClear_Click(object sender, EventArgs e)
    {
        try
        {
            if (_isConverting)
            {
                MessageBox.Show("转换正在进行中，无法清空", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ClearAllFields();
            _logger.LogInformation("清空所有字段");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空字段时发生错误");
        }
    }

    /// <summary>
    /// 查看OFD按钮点击事件
    /// </summary>
    private void BtnViewOfd_Click(object sender, EventArgs e)
    {
        try
        {
            // 检查是否有可用的OFD文件
            string? ofdFilePath = null;
            
            // 优先使用输出文件路径（如果存在）
            if (!string.IsNullOrEmpty(txtOutputFile.Text) && File.Exists(txtOutputFile.Text))
            {
                ofdFilePath = txtOutputFile.Text;
            }
            // 其次检查输入文件是否为OFD格式
            else if (!string.IsNullOrEmpty(txtInputFile.Text) && 
                     Path.GetExtension(txtInputFile.Text).ToLowerInvariant() == ".ofd" && 
                     File.Exists(txtInputFile.Text))
            {
                ofdFilePath = txtInputFile.Text;
            }
            
            if (!string.IsNullOrEmpty(ofdFilePath))
            {
                // 直接打开指定的OFD文件
                OpenOfdViewer(ofdFilePath);
            }
            else
            {
                // 没有现成的OFD文件，打开文件选择对话框
                var ofdDialog = new OpenFileDialog
                {
                    Title = "选择要查看的OFD文档",
                    Filter = "OFD文件 (*.ofd)|*.ofd|所有文件 (*.*)|*.*",
                    DefaultExt = "ofd"
                };
                
                if (ofdDialog.ShowDialog() == DialogResult.OK)
                {
                    OpenOfdViewer(ofdDialog.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开OFD查看器时发生错误");
            MessageBox.Show($"打开OFD查看器失败: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 自动检测转换类型
    /// </summary>
    private void AutoDetectConversionType(string filePath)
    {
        var conversionType = FileTypeDetector.GetConversionType(filePath);
        
        switch (conversionType)
        {
            case ConversionType.WordToOfd:
                rbWordToOfd.Checked = true;
                break;
            case ConversionType.HtmlToOfd:
                rbHtmlToOfd.Checked = true;
                break;
            case ConversionType.PdfToOfd:
                rbPdfToOfd.Checked = true;
                break;
            default:
                MessageBox.Show("不支持的文件格式", "警告", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                break;
        }
    }

    /// <summary>
    /// 自动生成输出文件名
    /// </summary>
    private void AutoGenerateOutputFileName(string inputFilePath)
    {
        var directory = Path.GetDirectoryName(inputFilePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
        var outputFileName = Path.Combine(directory!, $"{fileNameWithoutExtension}.ofd");
        
        txtOutputFile.Text = outputFileName;
    }

    /// <summary>
    /// 显示文件信息
    /// </summary>
    private void DisplayFileInfo(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            var info = $"文件名: {fileInfo.Name}\n" +
                      $"文件大小: {FormatFileSize(fileInfo.Length)}\n" +
                      $"文件类型: {extension}\n" +
                      $"修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n";

            // 根据文件类型添加特定信息
            switch (extension)
            {
                case ".pdf":
                    var pdfInfo = Pdf2OfdConverter.GetDocumentInfo(filePath);
                    info += $"页面数量: {pdfInfo.PageCount}\n";
                    if (!string.IsNullOrEmpty(pdfInfo.Title))
                        info += $"文档标题: {pdfInfo.Title}\n";
                    break;
            }

            UpdateFileInfo(info);
        }
        catch (Exception ex)
        {
            UpdateFileInfo($"无法读取文件信息: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(txtInputFile.Text))
        {
            MessageBox.Show("请选择输入文件", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (!File.Exists(txtInputFile.Text))
        {
            MessageBox.Show("输入文件不存在", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtOutputFile.Text))
        {
            MessageBox.Show("请指定输出文件路径", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // 检查输出文件夹是否存在
        var outputDir = Path.GetDirectoryName(txtOutputFile.Text);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法创建输出目录: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 开始转换
    /// </summary>
    private async Task StartConversion()
    {
        _isConverting = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // 更新UI状态
        SetConversionUI(true);
        
        var inputFile = txtInputFile.Text;
        var outputFile = txtOutputFile.Text;
        
        UpdateStatus("开始转换...");
        
        ConversionResult? result = null;
        var startTime = DateTime.Now;
        
        try
        {
            // 根据选择的转换类型执行转换
            if (rbWordToOfd.Checked && _wordConverter != null)
            {
                result = await _wordConverter.ConvertAsync(inputFile, outputFile, _cancellationTokenSource.Token);
            }
            else if (rbHtmlToOfd.Checked && _htmlConverter != null)
            {
                result = await _htmlConverter.ConvertAsync(inputFile, outputFile, _cancellationTokenSource.Token);
            }
            else if (rbPdfToOfd.Checked && _pdfConverter != null)
            {
                result = await _pdfConverter.ConvertAsync(inputFile, outputFile, _cancellationTokenSource.Token);
            }
            
            var duration = DateTime.Now - startTime;
            
            if (result != null)
            {
                result.Duration = duration;
                result.StartTime = startTime;
                result.EndTime = DateTime.Now;
                
                ShowConversionResult(result);
            }
        }
        finally
        {
            ResetConversionUI();
        }
    }

    /// <summary>
    /// 显示转换结果
    /// </summary>
    private void ShowConversionResult(ConversionResult result)
    {
        if (result.Success)
        {
            var message = $"转换成功完成！\n\n" +
                         $"输出文件: {result.OutputPath}\n" +
                         $"输入文件大小: {FormatFileSize(result.InputFileSize)}\n" +
                         $"输出文件大小: {FormatFileSize(result.OutputFileSize)}\n" +
                         $"转换耗时: {result.Duration?.TotalSeconds:F2} 秒";
            
            if (result.PageCount.HasValue)
            {
                message += $"\n页面数量: {result.PageCount.Value}";
            }
            
            MessageBox.Show(message, "转换成功", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            UpdateStatus("转换完成");
            _logger.LogInformation("转换成功: {OutputFile}", result.OutputPath);
        }
        else
        {
            MessageBox.Show($"转换失败: {result.ErrorMessage}", "转换失败", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            UpdateStatus("转换失败");
            _logger.LogError("转换失败: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// 转换器进度变化事件处理
    /// </summary>
    private void OnConverterProgressChanged(object? sender, ConversionProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnConverterProgressChanged(sender, e)));
            return;
        }
        
        progressBar.Value = Math.Min(Math.Max(e.Percentage, 0), 100);
        lblProgressMessage.Text = e.Message;
        UpdateStatus(e.Message);
        
        toolStripProgressBar.Value = progressBar.Value;
    }

    /// <summary>
    /// 转换器完成事件处理
    /// </summary>
    private void OnConverterCompleted(object? sender, ConversionCompletedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnConverterCompleted(sender, e)));
            return;
        }
        
        // 事件处理在StartConversion方法中统一处理
    }

    /// <summary>
    /// 设置转换时的UI状态
    /// </summary>
    private void SetConversionUI(bool isConverting)
    {
        btnConvert.Enabled = !isConverting;
        btnCancel.Enabled = isConverting;
        btnBrowseInput.Enabled = !isConverting;
        btnBrowseOutput.Enabled = !isConverting;
        grpConversionType.Enabled = !isConverting;
        
        progressBar.Visible = isConverting;
        toolStripProgressBar.Visible = isConverting;
        
        if (isConverting)
        {
            progressBar.Value = 0;
            toolStripProgressBar.Value = 0;
        }
    }

    /// <summary>
    /// 重置转换UI状态
    /// </summary>
    private void ResetConversionUI()
    {
        _isConverting = false;
        SetConversionUI(false);
        
        progressBar.Value = 0;
        lblProgressMessage.Text = "准备就绪";
        toolStripProgressBar.Value = 0;
        
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    /// <summary>
    /// 清空所有字段
    /// </summary>
    private void ClearAllFields()
    {
        txtInputFile.Clear();
        txtOutputFile.Clear();
        rbWordToOfd.Checked = true;
        
        progressBar.Value = 0;
        lblProgressMessage.Text = "准备就绪";
        
        UpdateFileInfo("请选择要转换的文件...");
        UpdateStatus("准备就绪");
    }

    /// <summary>
    /// 更新状态栏
    /// </summary>
    private void UpdateStatus(string message)
    {
        toolStripStatusLabel.Text = message;
    }

    /// <summary>
    /// 更新文件信息
    /// </summary>
    private void UpdateFileInfo(string info)
    {
        txtFileInfo.Text = info;
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
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
    /// 打开OFD查看器
    /// </summary>
    /// <param name="ofdFilePath">OFD文件路径</param>
    private void OpenOfdViewer(string ofdFilePath)
    {
        try
        {
            _logger.LogInformation("打开OFD查看器: {FilePath}", ofdFilePath);
            
            // 检查文件是否存在
            if (!File.Exists(ofdFilePath))
            {
                MessageBox.Show("指定的OFD文件不存在", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // 检查文件扩展名
            var extension = Path.GetExtension(ofdFilePath).ToLowerInvariant();
            if (extension != ".ofd")
            {
                var result = MessageBox.Show(
                    $"选择的文件不是OFD格式（{extension}）。\n是否要尝试打开？", 
                    "文件格式警告", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
                    
                if (result == DialogResult.No)
                {
                    return;
                }
            }
            
            // 验证OFD文件是否有效
            var validationResult = ValidateOfdFile(ofdFilePath);
            if (!validationResult.IsValid)
            {
                var result = MessageBox.Show(
                    $"OFD文件验证失败：\n{validationResult.ErrorMessage}\n\n是否要强制尝试打开？", 
                    "文件验证失败", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Warning);
                    
                if (result == DialogResult.No)
                {
                    return;
                }
            }
            
            // 创建并显示OFD查看器窗体
            var viewerForm = new OfdViewerForm(ofdFilePath);
            viewerForm.Show();
            
            UpdateStatus($"已打开OFD查看器 - {Path.GetFileName(ofdFilePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开OFD查看器失败: {FilePath}", ofdFilePath);
            MessageBox.Show($"打开OFD查看器失败: {ex.Message}", "错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 验证OFD文件是否有效
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>验证结果</returns>
    private (bool IsValid, string ErrorMessage) ValidateOfdFile(string filePath)
    {
        try
        {
            // 检查文件大小
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return (false, "文件大小为0字节，可能是空文件");
            }
            
            if (fileInfo.Length < 22) // ZIP文件最小大小
            {
                return (false, "文件太小，不是有效的OFD文件");
            }
            
            // 尝试作为ZIP文件打开
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // 检查ZIP文件头
                var buffer = new byte[4];
                fileStream.Read(buffer, 0, 4);
                
                // ZIP文件的魔数：0x504B0304 (PK..)
                if (buffer[0] != 0x50 || buffer[1] != 0x4B)
                {
                    return (false, "文件不是有效的ZIP格式，OFD文件必须是ZIP压缩包");
                }
                
                // 尝试使用System.IO.Compression验证ZIP结构
                fileStream.Position = 0;
                using (var archive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Read))
                {
                    // 检查是否包含OFD.xml文件
                    var ofdXmlEntry = archive.Entries.FirstOrDefault(e => 
                        e.FullName.Equals("OFD.xml", StringComparison.OrdinalIgnoreCase));
                    
                    if (ofdXmlEntry == null)
                    {
                        return (false, "文件中缺少OFD.xml根文件，不是有效的OFD文档");
                    }
                    
                    // 检查Doc目录
                    var hasDocDir = archive.Entries.Any(e => 
                        e.FullName.StartsWith("Doc_", StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.StartsWith("Doc/", StringComparison.OrdinalIgnoreCase));
                    
                    if (!hasDocDir)
                    {
                        return (false, "文件中缺少Doc目录，OFD文档结构不完整");
                    }
                }
            }
            
            return (true, string.Empty);
        }
        catch (InvalidDataException ex)
        {
            return (false, $"ZIP文件结构损坏：{ex.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "没有读取文件的权限");
        }
        catch (Exception ex)
        {
            return (false, $"文件验证失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 窗体关闭时的清理工作
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            if (_isConverting)
            {
                var result = MessageBox.Show("转换正在进行中，确定要退出吗？", "确认", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                
                _cancellationTokenSource?.Cancel();
            }
            
            // 释放转换器资源
            _wordConverter?.Dispose();
            _htmlConverter?.Dispose();
            _pdfConverter?.Dispose();
            
            _cancellationTokenSource?.Dispose();
            
            _logger.LogInformation("应用程序正常退出");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "窗体关闭时发生错误");
        }
        
        base.OnFormClosing(e);
    }
}