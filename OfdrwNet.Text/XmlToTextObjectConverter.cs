using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Text;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.Graph;

namespace OfdrwNet.Text;

/// <summary>
/// XML元素到文本对象的转换器
/// </summary>
public sealed class XmlToTextObjectConverter
{
    private readonly ILogger? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public XmlToTextObjectConverter(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 将 XElement 转换为 TextObject
    /// </summary>
    /// <param name="textElement">文本对象的 XElement</param>
    /// <param name="nextId">ID 生成器</param>
    /// <returns>TextObject 实例</returns>
    public TextObject? ConvertXElementToTextObject(XElement textElement, Func<int> nextId)
    {
        try
        {
            // 解析基本属性
            var id = textElement.Attribute("ID")?.Value;
            var fontRef = textElement.Attribute("Font")?.Value;
            var size = float.TryParse(textElement.Attribute("Size")?.Value, out var sizeVal) ? sizeVal : 12f;
            var boundary = textElement.Attribute("Boundary")?.Value;
            var stroke = bool.TryParse(textElement.Attribute("Stroke")?.Value, out var strokeVal) && strokeVal;
            var fill = !bool.TryParse(textElement.Attribute("Fill")?.Value, out var fillVal) || fillVal;

            if (string.IsNullOrEmpty(fontRef) || string.IsNullOrEmpty(boundary))
            {
                _logger?.LogWarning("[XmlToTextObjectConverter] 缺少必要属性: Font={Font}, Boundary={Boundary}",
                    fontRef, boundary);
                return null;
            }

            // 解析边界框
            var boundaryParts = boundary.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (boundaryParts.Length < 4)
            {
                _logger?.LogWarning("[XmlToTextObjectConverter] 边界框格式错误: {Boundary}", boundary);
                return null;
            }

            var x = double.TryParse(boundaryParts[0], out var xVal) ? xVal : 0;
            var y = double.TryParse(boundaryParts[1], out var yVal) ? yVal : 0;
            var width = double.TryParse(boundaryParts[2], out var widthVal) ? widthVal : 0;
            var height = double.TryParse(boundaryParts[3], out var heightVal) ? heightVal : 0;

            // 创建 TextObject
            var textObject = new TextObject(new StRefId(int.TryParse(id, out var idVal) ? idVal : nextId()));
            textObject.SetFont(new StRefId(int.TryParse(fontRef, out var fontRefVal) ? fontRefVal : 1));
            textObject.SetSize(size);
            textObject.SetBoundary(x, y, width, height);
            textObject.SetStroke(stroke);
            textObject.SetFill(fill);

            // 设置填充颜色
            if (fill)
            {
                textObject.SetFillColor(CreateBlackFillColor());
            }

            // 设置描边颜色
            if (stroke)
            {
                textObject.SetStrokeColor(CreateBlackStrokeColor());
            }

            // 解析 CTM（如果有）
            ParseAndSetCtm(textElement, textObject);

            // 解析 TextCode 元素
            ParseTextCodes(textElement, textObject);

            _logger?.LogDebug("[XmlToTextObjectConverter] 成功转换文本对象: ID={Id}, 字符数={CharCount}",
                idVal, textObject.GetTextCodes()?.Count ?? 0);

            return textObject;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[XmlToTextObjectConverter] 转换文本对象失败");
            return null;
        }
    }

    /// <summary>
    /// 解析并设置变换矩阵
    /// </summary>
    /// <param name="textElement">文本元素</param>
    /// <param name="textObject">文本对象</param>
    private void ParseAndSetCtm(XElement textElement, TextObject textObject)
    {
        var ctmAttr = textElement.Attribute("CTM")?.Value;
        if (string.IsNullOrEmpty(ctmAttr))
            return;

        var ctmParts = ctmAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (ctmParts.Length < 6)
        {
            _logger?.LogWarning("[XmlToTextObjectConverter] CTM格式错误: {CTM}", ctmAttr);
            return;
        }

        var ctmValues = new double[6];
        bool allParsed = true;
        for (int i = 0; i < 6; i++)
        {
            if (!double.TryParse(ctmParts[i], out ctmValues[i]))
            {
                allParsed = false;
                break;
            }
        }

        if (allParsed)
        {
            var ctm = CreateCtm(ctmValues);
            if (ctm != null)
            {
                textObject.SetCtm(ctm);
            }
        }
        else
        {
            _logger?.LogWarning("[XmlToTextObjectConverter] CTM解析失败: {CTM}", ctmAttr);
        }
    }

    /// <summary>
    /// 解析文本代码元素
    /// </summary>
    /// <param name="textElement">文本元素</param>
    /// <param name="textObject">文本对象</param>
    private void ParseTextCodes(XElement textElement, TextObject textObject)
    {
        var textCodeElements = textElement.Elements("TextCode");
        foreach (var tcElement in textCodeElements)
        {
            var textCode = CreateTextCode(tcElement);
            if (textCode != null)
            {
                textObject.AddTextCode(textCode);
            }
        }
    }

    /// <summary>
    /// 创建文本代码对象
    /// </summary>
    /// <param name="tcElement">文本代码元素</param>
    /// <returns>文本代码对象</returns>
    private TextCode? CreateTextCode(XElement tcElement)
    {
        var tcX = double.TryParse(tcElement.Attribute("X")?.Value, out var tcXVal) ? tcXVal : 0;
        var tcY = double.TryParse(tcElement.Attribute("Y")?.Value, out var tcYVal) ? tcYVal : 0;
        var tcText = tcElement.Value ?? string.Empty;

        if (string.IsNullOrEmpty(tcText))
            return null;

        var textCode = new TextCode()
            .SetCoordinate(tcX, tcY)
            .SetContent(tcText);

        // 解析 DeltaX（如果有）
        ParseAndSetDeltaX(tcElement, textCode);

        // 解析 DeltaY（如果有）
        ParseAndSetDeltaY(tcElement, textCode);

        return textCode;
    }

    /// <summary>
    /// 解析并设置DeltaX
    /// </summary>
    /// <param name="tcElement">文本代码元素</param>
    /// <param name="textCode">文本代码对象</param>
    private void ParseAndSetDeltaX(XElement tcElement, TextCode textCode)
    {
        var deltaXAttr = tcElement.Attribute("DeltaX")?.Value;
        if (string.IsNullOrEmpty(deltaXAttr))
            return;

        var deltaXValues = ParseDoubleArray(deltaXAttr);
        if (deltaXValues.Length > 0)
        {
            textCode.SetDeltaX(new StArray(deltaXValues));
        }
    }

    /// <summary>
    /// 解析并设置DeltaY
    /// </summary>
    /// <param name="tcElement">文本代码元素</param>
    /// <param name="textCode">文本代码对象</param>
    private void ParseAndSetDeltaY(XElement tcElement, TextCode textCode)
    {
        var deltaYAttr = tcElement.Attribute("DeltaY")?.Value;
        if (string.IsNullOrEmpty(deltaYAttr))
            return;

        var deltaYValues = ParseDoubleArray(deltaYAttr);
        if (deltaYValues.Length > 0)
        {
            textCode.SetDeltaY(new StArray(deltaYValues));
        }
    }

    /// <summary>
    /// 解析双精度数组
    /// </summary>
    /// <param name="valueStr">值字符串</param>
    /// <returns>双精度数组</returns>
    private double[] ParseDoubleArray(string valueStr)
    {
        var parts = valueStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var values = new List<double>();

        foreach (var part in parts)
        {
            if (double.TryParse(part, out var val))
                values.Add(val);
        }

        return values.ToArray();
    }

    /// <summary>
    /// 创建变换矩阵
    /// </summary>
    /// <param name="ctm">变换矩阵数组</param>
    /// <returns>StArray或null</returns>
    private static StArray? CreateCtm(double[]? ctm)
    {
        if (ctm == null || ctm.Length < 6)
            return null;

        // 验证矩阵有效性（避免无效变换）
        if (ctm[0] == 0 && ctm[3] == 0) // 缩放为0
            return null;

        return new StArray(ctm);
    }

    /// <summary>
    /// 创建黑色描边颜色
    /// </summary>
    /// <returns>描边颜色</returns>
    private static CtColor CreateBlackStrokeColor() => CreateRgbColor(0, 0, 0, isStroke: true);

    /// <summary>
    /// 创建黑色填充颜色
    /// </summary>
    /// <returns>填充颜色</returns>
    private static CtColor CreateBlackFillColor() => CreateRgbColor(0, 0, 0, isStroke: false);

    /// <summary>
    /// 创建RGB颜色
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <param name="isStroke">是否为描边颜色</param>
    /// <returns>颜色对象</returns>
    private static CtColor CreateRgbColor(int r, int g, int b, bool isStroke)
    {
        CtColor color = isStroke ? new StrokeColor() : new FillColor();
        color.SetValue(new StArray(r, g, b));
        color.SetAlpha(255);
        color.AddAttribute("ColorSpace", "RGB");
        return color;
    }

    /// <summary>
    /// 批量转换文本对象
    /// </summary>
    /// <param name="textElements">文本元素集合</param>
    /// <param name="startId">起始ID</param>
    /// <returns>文本对象集合</returns>
    public List<TextObject> ConvertMultiple(IEnumerable<XElement> textElements, int startId = 1)
    {
        var results = new List<TextObject>();
        var currentId = startId;

        foreach (var element in textElements)
        {
            var textObject = ConvertXElementToTextObject(element, () => currentId++);
            if (textObject != null)
            {
                results.Add(textObject);
            }
        }

        _logger?.LogDebug("[XmlToTextObjectConverter] 批量转换完成: 输入={Input}, 成功={Success}",
            textElements.Count(), results.Count);

        return results;
    }
}
