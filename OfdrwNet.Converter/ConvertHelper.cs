using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Converter.Options;
using OfdrwNet.Converter.Core;

namespace OfdrwNet.Converter;

/// <summary>
/// 转换工具类（C#版）- 门面模式
/// 对应 Java org.ofdrw.converter.ConvertHelper
/// 提供 OFD <-> PDF 转换的统一入口，内部委托给具体的转换器实现。
/// 保持对外接口的向后兼容性。
/// </summary>
public static class ConvertHelper
{
    public const double Pt2Mm = 25.4 / 72.0; // 改为 public 供监听器访问

    // 内部转换器实例
    private static readonly OfdToPdfConverter _ofdToPdfConverter = new();
    private static readonly PdfToOfdConverter _pdfToOfdConverter = new();

    /// <summary>
    /// 转换使用库枚举（预留）
    /// </summary>
    public enum Lib
    {
        IText,
        PDFBox
    }

    /// <summary>
    /// 当前使用的转换库（默认 IText 语义）
    /// </summary>
    public static Lib CurrentLib { get; private set; } = Lib.IText;

    /// <summary>
    /// 使用 IText 兼容实现
    /// </summary>
    public static void UseIText()
    {
        CurrentLib = Lib.IText;
        OfdToPdfConverter.UseIText();
    }

    /// <summary>
    /// 使用 PDFBox 兼容实现
    /// </summary>
    public static void UsePDFBox()
    {
        CurrentLib = Lib.PDFBox;
        OfdToPdfConverter.UsePDFBox();
    }

    #region 公共入口（与 Java ofd2pdf(Object,Object) 语义对应）

    /// <summary>
    /// OFD 转 PDF 通用入口（不建议直接使用，建议调用具体重载）
    /// 支持输入：Stream, string(文件路径)
    /// 支持输出：Stream, string(文件路径)
    /// </summary>
    [Obsolete("请使用 ToPdf/ToPdfAsync 强类型重载。")]
    public static void Ofd2Pdf(object input, object output)
    {
        // 委托给 OfdToPdfConverter - 使用类型检查处理多态参数
        if (input is Stream inputStream && output is Stream outputStream)
        {
            _ofdToPdfConverter.ConvertToPdf(inputStream, outputStream, null);
        }
        else if (input is Stream inputStreamToFile && output is string outputPath)
        {
            _ofdToPdfConverter.ConvertToPdf(inputStreamToFile, outputPath, null);
        }
        else if (input is string inputPath && output is Stream outputStreamFromFile)
        {
            _ofdToPdfConverter.ConvertToPdf(inputPath, outputStreamFromFile, null);
        }
        else if (input is string inputPathToFile && output is string outputPathFromFile)
        {
            _ofdToPdfConverter.ConvertToPdf(inputPathToFile, outputPathFromFile, null);
        }
        else
        {
            throw new ArgumentException("不支持的输入输出格式组合");
        }
    }

    #endregion

    #region OFD 到 PDF 转换方法（强类型同步重载）

    /// <summary>
    /// 同步将 OFD 流转换为 PDF 流
    /// </summary>
    public static void ToPdf(Stream input, Stream output, PdfExportOptions? options = null)
    {
        _ofdToPdfConverter.ConvertToPdf(input, output, options);
    }

    /// <summary>
    /// 同步将 OFD 流转换为 PDF 文件
    /// </summary>
    public static void ToPdf(Stream input, string outputPath, PdfExportOptions? options = null)
    {
        _ofdToPdfConverter.ConvertToPdf(input, outputPath, options);
    }

    /// <summary>
    /// 同步将 OFD 文件转换为 PDF 流
    /// </summary>
    public static void ToPdf(string inputPath, Stream output, PdfExportOptions? options = null)
    {
        _ofdToPdfConverter.ConvertToPdf(inputPath, output, options);
    }

    /// <summary>
    /// 同步将 OFD 文件转换为 PDF 文件
    /// </summary>
    public static void ToPdf(string inputPath, string outputPath, PdfExportOptions? options = null)
    {
        _ofdToPdfConverter.ConvertToPdf(inputPath, outputPath, options);
    }

    /// <summary>
    /// 从已解压的 OFD 目录转换为 PDF
    /// </summary>
    public static void ToPdfFromUnzipped(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null)
    {
        _ofdToPdfConverter.ConvertFromUnzipped(unzippedPathRoot, outputPath, deleteOnClose, options);
    }

    #endregion

    #region OFD 到 PDF 转换方法（强类型异步重载）

    /// <summary>
    /// 异步将 OFD 流转换为 PDF 流
    /// </summary>
    public static Task ToPdfAsync(Stream input, Stream output, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return _ofdToPdfConverter.ConvertToPdfAsync(input, output, options, token);
    }

    /// <summary>
    /// 异步将 OFD 流转换为 PDF 文件
    /// </summary>
    public static Task ToPdfAsync(Stream input, string outputPath, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return _ofdToPdfConverter.ConvertToPdfAsync(input, outputPath, options, token);
    }

    /// <summary>
    /// 异步将 OFD 文件转换为 PDF 流
    /// </summary>
    public static Task ToPdfAsync(string inputPath, Stream output, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return _ofdToPdfConverter.ConvertToPdfAsync(inputPath, output, options, token);
    }

    /// <summary>
    /// 异步将 OFD 文件转换为 PDF 文件
    /// </summary>
    public static Task ToPdfAsync(string inputPath, string outputPath, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return _ofdToPdfConverter.ConvertToPdfAsync(inputPath, outputPath, options, token);
    }

    /// <summary>
    /// 从已解压的 OFD 目录异步转换为 PDF
    /// </summary>
    public static Task ToPdfFromUnzippedAsync(string unzippedPathRoot, string outputPath, bool deleteOnClose, PdfExportOptions? options = null, CancellationToken token = default)
    {
        return _ofdToPdfConverter.ConvertFromUnzippedAsync(unzippedPathRoot, outputPath, deleteOnClose, options, token);
    }

    #endregion

    #region PDF 到 OFD 转换方法

    /// <summary>
    /// 同步将 PDF 转换为 OFD
    /// </summary>
    public static void PdfToOfd(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        _pdfToOfdConverter.ConvertToOfd(pdfPath, ofdOutputDir, options);
    }

    /// <summary>
    /// 异步将 PDF 转换为 OFD
    /// </summary>
    public static Task PdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        return _pdfToOfdConverter.ConvertToOfdAsync(pdfPath, ofdOutputDir, options);
    }




    /// <summary>
    /// 兼容性方法：新增别名方法以兼容测试程序
    /// </summary>
    public static Task ConvertPdfToOfdAsync(string pdfPath, string ofdOutputDir, PdfToOfdOptions? options = null)
    {
        return _pdfToOfdConverter.ConvertPdfToOfdAsync(pdfPath, ofdOutputDir, options);
    }

    #endregion

    #region HTML 导出占位

    /// <summary>
    /// HTML 导出（占位，尚未实现）
    /// </summary>
    public static void ToHtml(object reader, string outputPath, int screenWidth)
    {
        throw new NotImplementedException("HTML 导出尚未在 .NET 版本实现");
    }

    /// <summary>
    /// HTML 导出（占位，尚未实现）
    /// </summary>
    public static void ToHtml(string ofdPath, string htmlOutputPath, int screenWidth)
    {
        throw new NotImplementedException("HTML 导出尚未在 .NET 版本实现");
    }

    #endregion

    #region 向后兼容的内部代理方法

    /// <summary>
    /// 字体归一逻辑（向后兼容的内部代理，后续可删除）
    /// </summary>
    internal static string NormalizeLogicalFontName(string baseName)
    {
        return ConvertUtils.NormalizeLogicalFontName(baseName);
    }

    #endregion
}
