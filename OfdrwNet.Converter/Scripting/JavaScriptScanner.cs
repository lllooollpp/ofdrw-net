using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OfdrwNet.Converter.Scripting;

/// <summary>
/// JavaScript 扫描器。
/// </summary>
/// <remarks>
/// 扫描 PDF 文档中的 JavaScript 脚本，不执行。
/// 生成脚本清单和报告。
/// FR-20: JavaScript 检测与报告
///
/// 扫描 4 个级别：
/// 1. 文档级：Catalog /Names /JavaScript 名称树
/// 2. 页面级：页面 /AA (附加动作) 字典
/// 3. 表单字段级：AcroForm 字段 /AA 字典
/// 4. 注释级：注释 /AA 字典
///
/// 当前为占位实现，使用反射访问 PDF 对象。
/// </remarks>
public sealed class JavaScriptScanner
{
    private readonly ILogger<JavaScriptScanner> _logger;
    private int _nextObjectId;

    /// <summary>
    /// 初始化 JavaScriptScanner 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public JavaScriptScanner(ILogger<JavaScriptScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nextObjectId = 1;
    }

    /// <summary>
    /// 扫描 PDF 文档中的所有 JavaScript 脚本。
    /// </summary>
    /// <param name="pdfDocument">PDF 文档对象（iText7 PdfDocument 或模拟对象）</param>
    /// <returns>脚本扫描结果</returns>
    public JavaScriptScanResult ScanDocument(object pdfDocument)
    {
        if (pdfDocument == null)
        {
            throw new ArgumentNullException(nameof(pdfDocument));
        }

        var result = new JavaScriptScanResult
        {
            Scripts = new List<JsScriptInfo>()
        };

        try
        {
            _logger.LogInformation("Scanning PDF document for JavaScript");

            // 扫描文档级脚本（Catalog /Names /JavaScript）
            ScanDocumentLevelScripts(pdfDocument, result);

            // 扫描页面级脚本（页面动作）
            ScanPageLevelScripts(pdfDocument, result);

            // 扫描表单字段脚本（附加动作）
            ScanFormFieldScripts(pdfDocument, result);

            // 扫描注释脚本
            ScanAnnotationScripts(pdfDocument, result);

            _logger.LogInformation(
                "JavaScript scan complete: found {Count} scripts ({DocLevel} doc-level, {PageLevel} page-level, {FormLevel} form-level, {AnnotLevel} annot-level)",
                result.Scripts.Count,
                result.Scripts.Count(s => s.ScriptType == "Document"),
                result.Scripts.Count(s => s.ScriptType == "Page"),
                result.Scripts.Count(s => s.ScriptType == "FormField"),
                result.Scripts.Count(s => s.ScriptType == "Annotation"));

            if (result.Scripts.Count > 0)
            {
                _logger.LogWarning("Embedded JavaScript removed (count: {Count})", result.Scripts.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan PDF document for JavaScript");
            return result;
        }
    }

    /// <summary>
    /// 扫描文档级脚本（Catalog /Names /JavaScript 名称树）。
    /// </summary>
    private void ScanDocumentLevelScripts(object pdfDocument, JavaScriptScanResult result)
    {
        try
        {
            // 占位实现：模拟从 Catalog 获取 JavaScript 名称树
            _logger.LogDebug("Scanning document-level JavaScript");

            // 实际实现应访问：
            // PdfCatalog catalog = pdfDocument.GetCatalog();
            // PdfDictionary names = catalog.GetNameTree(PdfName.JavaScript);
            // 遍历名称树提取脚本
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan document-level scripts");
        }
    }

    /// <summary>
    /// 扫描页面级脚本（页面 /AA 动作）。
    /// </summary>
    private void ScanPageLevelScripts(object pdfDocument, JavaScriptScanResult result)
    {
        try
        {
            _logger.LogDebug("Scanning page-level JavaScript");

            // 占位实现：模拟遍历所有页面
            // 实际实现应：
            // for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            // {
            //     PdfPage page = pdfDocument.GetPage(i);
            //     PdfDictionary aa = page.GetPdfObject().GetAsDictionary(PdfName.AA);
            //     ExtractScriptsFromActions(aa, "Page", i);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan page-level scripts");
        }
    }

    /// <summary>
    /// 扫描表单字段脚本（AcroForm /Fields /AA）。
    /// </summary>
    private void ScanFormFieldScripts(object pdfDocument, JavaScriptScanResult result)
    {
        try
        {
            _logger.LogDebug("Scanning form field JavaScript");

            // 占位实现：模拟遍历表单字段
            // 实际实现应：
            // PdfAcroForm acroForm = PdfFormCreator.GetAcroForm(pdfDocument, false);
            // if (acroForm != null)
            // {
            //     foreach (var field in acroForm.GetAllFormFields())
            //     {
            //         PdfDictionary aa = field.Value.GetPdfObject().GetAsDictionary(PdfName.AA);
            //         ExtractScriptsFromActions(aa, "FormField", field.Key);
            //     }
            // }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan form field scripts");
        }
    }

    /// <summary>
    /// 扫描注释脚本（注释 /AA）。
    /// </summary>
    private void ScanAnnotationScripts(object pdfDocument, JavaScriptScanResult result)
    {
        try
        {
            _logger.LogDebug("Scanning annotation JavaScript");

            // 占位实现：模拟遍历所有注释
            // 实际实现应：
            // for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            // {
            //     PdfPage page = pdfDocument.GetPage(i);
            //     List<PdfAnnotation> annotations = page.GetAnnotations();
            //     foreach (var annot in annotations)
            //     {
            //         PdfDictionary aa = annot.GetPdfObject().GetAsDictionary(PdfName.AA);
            //         ExtractScriptsFromActions(aa, "Annotation", $"Page{i}_{annot.GetType().Name}");
            //     }
            // }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to scan annotation scripts");
        }
    }

    /// <summary>
    /// 从动作字典中提取 JavaScript 脚本。
    /// </summary>
    /// <param name="actionDict">动作字典（/AA 或 /A）</param>
    /// <param name="scriptType">脚本类型（Document, Page, FormField, Annotation）</param>
    /// <param name="context">上下文信息</param>
    /// <returns>提取的脚本列表</returns>
    private IList<JsScriptInfo> ExtractScriptsFromActions(object? actionDict, string scriptType, string context)
    {
        var scripts = new List<JsScriptInfo>();

        if (actionDict == null)
        {
            return scripts;
        }

        try
        {
            // 占位实现：模拟从动作字典提取脚本
            // 实际实现应遍历以下键：
            // - /AA: Calculate, Validate, Format, Keystroke (表单字段)
            // - /AA: C, F, K, V (简写)
            // - /A: 单个动作
            //
            // 每个动作检查 /S == /JavaScript，然后提取 /JS 键
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract scripts from action dictionary for {Type}:{Context}", scriptType, context);
        }

        return scripts;
    }

    /// <summary>
    /// 从单个动作中提取 JavaScript 内容。
    /// </summary>
    /// <param name="action">动作对象</param>
    /// <returns>JavaScript 内容，如果不是 JavaScript 动作则返回 null</returns>
    private string? ExtractJavaScriptFromAction(object action)
    {
        if (action == null)
        {
            return null;
        }

        try
        {
            // 占位实现：模拟提取 JavaScript
            // 实际实现应：
            // if (action.Get(PdfName.S) == PdfName.JavaScript)
            // {
            //     PdfObject js = action.Get(PdfName.JS);
            //     if (js is PdfString str)
            //         return str.GetValue();
            //     else if (js is PdfStream stream)
            //         return Encoding.UTF8.GetString(stream.GetBytes());
            // }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract JavaScript from action");
        }

        return null;
    }

    /// <summary>
    /// 创建脚本信息对象。
    /// </summary>
    /// <param name="content">脚本内容</param>
    /// <param name="scriptType">脚本类型</param>
    /// <returns>JsScriptInfo 对象</returns>
    private JsScriptInfo CreateScriptInfo(string content, string scriptType)
    {
        var truncatedContent = content.Length > 200 ? content.Substring(0, 200) : content;

        return new JsScriptInfo
        {
            ObjectId = _nextObjectId++,
            Length = content.Length,
            Sha256 = ComputeSha256Hash(content),
            Snippet = truncatedContent,
            ScriptType = scriptType
        };
    }

    /// <summary>
    /// 计算 SHA-256 哈希值。
    /// </summary>
    private string ComputeSha256Hash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// JavaScript 扫描结果。
/// </summary>
public sealed class JavaScriptScanResult
{
    /// <summary>
    /// 扫描到的脚本列表。
    /// </summary>
    public IList<JsScriptInfo> Scripts { get; set; } = new List<JsScriptInfo>();
}
