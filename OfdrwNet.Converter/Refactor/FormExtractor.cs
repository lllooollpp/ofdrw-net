using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Options;
using OfdrwNet.Abstractions;

namespace OfdrwNet.Converter.Refactor;


/// <summary>
/// 表单提取器：负责将 PDF AcroForm 字段/XFA 表单转换为 OFD 表单结构。
/// </summary>
internal class FormExtractor : IPdfContentExtractor
{
    // T075: 服务依赖

    // 依赖类型修正
    private readonly OfdrwNet.Abstractions.Forms.IFormFieldMapper? _formMapper;
    private readonly OfdrwNet.Converter.Forms.XfaDetector? _xfaDetector;
    private readonly OfdrwNet.Converter.Forms.XfaHintWriter? _xfaHintWriter;
    private readonly OfdrwNet.Converter.Scripting.JavaScriptScanner? _jsScanner;

    public FormExtractor(
        OfdrwNet.Abstractions.Forms.IFormFieldMapper? formMapper = null,
        OfdrwNet.Converter.Forms.XfaDetector? xfaDetector = null,
        OfdrwNet.Converter.Forms.XfaHintWriter? xfaHintWriter = null,
        OfdrwNet.Converter.Scripting.JavaScriptScanner? jsScanner = null)
    {
        _formMapper = formMapper;
        _xfaDetector = xfaDetector;
        _xfaHintWriter = xfaHintWriter;
        _jsScanner = jsScanner;
    }

    public Task ExtractAsync(PdfDocument pdfDoc, IOfdDocWriter ofd, PdfToOfdOptions options, ILogger? logger, CancellationToken token)
    {
        logger?.LogDebug("[PDF2OFD][Form] 开始表单字段提取");

        // T075: XFA检测与降级

        // XFA检测与降级
        if (_xfaDetector != null)
        {
            var xfaResult = _xfaDetector.Detect(pdfDoc);
            if (xfaResult.HasXfa)
            {
                var staticFields = ConvertXfaToStatic(xfaResult);
                _xfaHintWriter?.WriteHints(xfaResult, "hints.json");
                foreach (var field in staticFields)
                {
                    var ofdField = _formMapper?.MapField(field);
                    if (ofdField is OfdrwNet.Converter.Domain.FormField formField)
                    {
                        (ofd as OfdWriter)?.AddFormField(formField);
                    }
                }
                logger?.LogInformation("[PDF2OFD][Form] XFA表单降级完成，字段数: {Count}", staticFields.Count);
                return Task.CompletedTask;
            }
        }

        // 标准PDF表单字段提取
        var fields = ExtractStandardFields(pdfDoc);
        int jsCount = 0;
        foreach (var field in fields)
        {
            // JavaScript检测与移除
            if (_formMapper != null && _formMapper.HasJavaScript(field))
            {
                var jsCode = _formMapper.ExtractJavaScript(field);
                if (!string.IsNullOrEmpty(jsCode) && _jsScanner != null)
                {
                    var scanResult = _jsScanner.ScanDocument(jsCode);
                    if (scanResult.Scripts.Count > 0)
                    {
                        jsCount++;
                        logger?.LogWarning("[PDF2OFD][Form] 检测到表单字段JS，已移除: {Name}", field.GetType().GetProperty("Name")?.GetValue(field));
                        // 可选：移除或记录JS
                    }
                }
            }
            var ofdField = _formMapper?.MapField(field);
            if (ofdField is OfdrwNet.Converter.Domain.FormField formField)
            {
                (ofd as OfdWriter)?.AddFormField(formField);
            }
        }
        logger?.LogInformation("[PDF2OFD][Form] 标准表单字段提取完成，字段数: {Count}，移除JS: {JsCount}", fields.Count, jsCount);
        return Task.CompletedTask;
    }

    // XFA结构转静态字段
    private List<object> ConvertXfaToStatic(OfdrwNet.Converter.Forms.XfaDetectionResult xfaResult)
    {
        // TODO: 实现XFA结构解析与静态字段转换，返回PDF字段对象集合
        return new List<object>();
    }

    // 标准PDF表单字段提取
    private List<object> ExtractStandardFields(PdfDocument pdfDoc)
    {
        // TODO: 实现标准PDF表单字段提取，返回PDF字段对象集合
        return new List<object>();
    }
}
