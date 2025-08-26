namespace OfdrwNet.WinFormsDemo.Converters;

/// <summary>
/// 转换进度事件参数
/// </summary>
public class ConversionProgressEventArgs : EventArgs
{
    /// <summary>
    /// 进度百分比 (0-100)
    /// </summary>
    public int Percentage { get; set; }
    
    /// <summary>
    /// 进度消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 转换完成事件参数
/// </summary>
public class ConversionCompletedEventArgs : EventArgs
{
    /// <summary>
    /// 转换结果
    /// </summary>
    public ConversionResult Result { get; set; } = new();
}

/// <summary>
/// 转换结果
/// </summary>
public class ConversionResult
{
    /// <summary>
    /// 转换是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 输出文件路径
    /// </summary>
    public string? OutputPath { get; set; }
    
    /// <summary>
    /// 输入文件大小（字节）
    /// </summary>
    public long InputFileSize { get; set; }
    
    /// <summary>
    /// 输出文件大小（字节）
    /// </summary>
    public long OutputFileSize { get; set; }
    
    /// <summary>
    /// 页面数量（如果适用）
    /// </summary>
    public int? PageCount { get; set; }
    
    /// <summary>
    /// 转换耗时
    /// </summary>
    public TimeSpan? Duration { get; set; }
    
    /// <summary>
    /// 转换开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// 转换结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// 支持的转换类型
/// </summary>
public enum ConversionType
{
    /// <summary>
    /// Word转OFD
    /// </summary>
    WordToOfd,
    
    /// <summary>
    /// HTML转OFD
    /// </summary>
    HtmlToOfd,
    
    /// <summary>
    /// PDF转OFD
    /// </summary>
    PdfToOfd
}

/// <summary>
/// 文件类型识别器
/// </summary>
public static class FileTypeDetector
{
    /// <summary>
    /// 根据文件扩展名获取转换类型
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>转换类型，如果不支持则返回null</returns>
    public static ConversionType? GetConversionType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".docx" => ConversionType.WordToOfd,
            ".html" or ".htm" => ConversionType.HtmlToOfd,
            ".pdf" => ConversionType.PdfToOfd,
            _ => null
        };
    }
    
    /// <summary>
    /// 获取支持的所有文件扩展名
    /// </summary>
    /// <returns>支持的扩展名列表</returns>
    public static string[] GetSupportedExtensions()
    {
        return new[] { ".docx", ".html", ".htm", ".pdf" };
    }
    
    /// <summary>
    /// 获取文件对话框的过滤器字符串
    /// </summary>
    /// <returns>文件对话框过滤器</returns>
    public static string GetFileDialogFilter()
    {
        return "支持的文档格式|*.docx;*.html;*.htm;*.pdf|" +
               "Word文档 (*.docx)|*.docx|" +
               "HTML文件 (*.html;*.htm)|*.html;*.htm|" +
               "PDF文件 (*.pdf)|*.pdf|" +
               "所有文件 (*.*)|*.*";
    }
    
    /// <summary>
    /// 验证文件是否支持转换
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否支持</returns>
    public static bool IsSupported(string filePath)
    {
        return GetConversionType(filePath) != null;
    }
}