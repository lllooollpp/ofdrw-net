using System;
using System.IO;
using System.Threading.Tasks;
using OfdrwNet.Layout.Element;

namespace OfdrwNet;

/// <summary>
/// OFDRW.NET 便捷API帮助类
/// 提供常用操作的静态方法
/// </summary>
public static class OfdrwHelper
{
    #region 文档创建

    /// <summary>
    /// 创建新的OFD文档
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>OFD文档对象</returns>
    public static OFDDoc CreateDocument(string outputPath)
    {
        return new OFDDoc(outputPath);
    }

    /// <summary>
    /// 创建新的OFD文档
    /// </summary>
    /// <param name="outputStream">输出流</param>
    /// <returns>OFD文档对象</returns>
    public static OFDDoc CreateDocument(Stream outputStream)
    {
        return new OFDDoc(outputStream);
    }

    #endregion

    #region 格式转换

    /// <summary>
    /// 将OFD文档转换为PDF
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="pdfPath">PDF输出路径</param>
    /// <returns>转换任务</returns>
    public static async Task ConvertToPdfAsync(string ofdPath, string pdfPath)
    {
        // 占位符实现
        await Task.CompletedTask;
    }

    /// <summary>
    /// 将OFD文档转换为图片
    /// </summary>
    /// <param name="ofdPath">OFD文件路径</param>
    /// <param name="outputDirectory">图片输出目录</param>
    /// <param name="imageFormat">图片格式 (png, jpg, etc.)</param>
    /// <param name="dpi">图片分辨率</param>
    /// <returns>转换任务</returns>
    public static async Task ConvertToImagesAsync(string ofdPath, string outputDirectory, 
        string imageFormat = "png", int dpi = 300)
    {
        // 占位符实现
        await Task.CompletedTask;
    }

    /// <summary>
    /// 将PDF文档转换为OFD
    /// </summary>
    /// <param name="pdfPath">PDF文件路径</param>
    /// <param name="ofdPath">OFD输出路径</param>
    /// <returns>转换任务</returns>
    public static async Task ConvertFromPdfAsync(string pdfPath, string ofdPath)
    {
        // 占位符实现
        await Task.CompletedTask;
    }

    #endregion

    #region 文档信息

    /// <summary>
    /// 获取OFD文档信息
    /// </summary>
    /// <param name="filePath">文档文件路径</param>
    /// <returns>文档信息</returns>
    public static async Task<DocumentInfo> GetDocumentInfoAsync(string filePath)
    {
        // 占位符实现
        await Task.CompletedTask;
        return new DocumentInfo
        {
            Title = "Sample Document",
            Author = "OFDRW.NET",
            PageCount = 1
        };
    }

    /// <summary>
    /// 获取OFD文档页面数量
    /// </summary>
    /// <param name="filePath">文档文件路径</param>
    /// <returns>页面数量</returns>
    public static async Task<int> GetPageCountAsync(string filePath)
    {
        // 占位符实现
        await Task.CompletedTask;
        return 1;
    }

    /// <summary>
    /// 检查文件是否为有效的OFD文档
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否为有效OFD文档</returns>
    public static async Task<bool> IsValidOfdDocumentAsync(string filePath)
    {
        try
        {
            // 简单的文件存在性检查
            await Task.CompletedTask;
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region HelloWorld示例

    /// <summary>
    /// 创建一个简单的HelloWorld OFD文档
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="message">要显示的消息</param>
    /// <returns>创建任务</returns>
    public static async Task CreateHelloWorldAsync(string outputPath, string message = "Hello, OFDRW.NET!")
    {
        using var doc = CreateDocument(outputPath);
        
        // 创建一个简单的文本段落
        var paragraph = new Paragraph(message)
        {
            FontSize = 4.23, // 12pt 转毫米
            FontName = "宋体",
            Color = "#000000",
            X = 20,
            Y = 20
        };

        // 添加到文档
        doc.Add(paragraph);
        
        // 保存文档
        await doc.CloseAsync();
    }

    #endregion
}

/// <summary>
/// 文档信息类
/// </summary>
public class DocumentInfo
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Creator { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ModificationDate { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public int PageCount { get; set; }
}