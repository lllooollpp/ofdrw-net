namespace OfdrwNet.Converter.Export;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using OfdrwNet.Reader;
using System.Xml.Linq;
using System.Globalization; // 新增: InvariantCulture 解析
using System.Text.Json; // 新增: JSON 输出
using System.Collections.Generic; // 新增: 字体缓存
using iText.IO.Image; // 新增 图片
using iText.IO.Font; // 新增 字体嵌入
using static iText.IO.Font.PdfEncodings; // 修正：使用 static 引入编码常量
using System.IO; // 新增 IO

/// <summary>
/// 简单统计结构
/// </summary>
internal class PdfExportSimpleStats
{
    public int Pages { get; set; }
    public int TextObjects { get; set; }
    public int TextCodes { get; set; }
    public int ImageObjects { get; set; }
    public int RotatedText { get; set; } // 新增：旋转/变换文本计数
    public int VectorObjects { get; set; } // 新增：矢量对象计数
    public int ImagesEmbedded { get; set; } // 新增：真实嵌入图片数
    public int FontsEmbedded { get; set; } // 新增：嵌入字体数
    public HashSet<string> Fonts { get; set; } = new();
}

/// <summary>
/// 页面生成器接口（便于后续扩展 PDFSharp 等实现）
/// </summary>
internal interface IPdfPageMaker : IDisposable
{
    void BeginDocument();
    void EndDocument();
    void MakePage(int zeroBasedPageIndex, PageInfo pageInfo);
    PdfExportSimpleStats GetStats();
}

/// <summary>
/// iText7 页面生成器：支持基础文本导出及保留布局模式（绝对定位）
/// </summary>
internal class ITextPageMaker : IPdfPageMaker
{
    private readonly PdfDocument _pdfDoc;
    private readonly Document _doc;
    private readonly bool _preserveLayout;
    private readonly PdfFont _defaultFont;
    private readonly Func<string, string?>? _fontMapper;
    private readonly Dictionary<string, PdfFont> _fontCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _embedFonts;
    private readonly bool _realImageEmbedding; // 占位
    private readonly PdfExportSimpleStats _stats = new();
    private readonly Func<int, bool>? _pageFilter; // 新增：页面过滤（1-based）
    private readonly string _ofdWorkDir; // 新增：工作目录用于资源定位

    public ITextPageMaker(PdfDocument pdfDoc, bool preserveLayout, Func<string, string?>? fontMapper = null, bool embedFonts = false, bool realImageEmbedding = false, Func<int,bool>? pageFilter = null, string? ofdWorkDir = null)
    {
        _pdfDoc = pdfDoc;
        _doc = new Document(_pdfDoc);
        _preserveLayout = preserveLayout;
        _defaultFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        _fontMapper = fontMapper;
        _embedFonts = embedFonts;
        _realImageEmbedding = realImageEmbedding;
        _pageFilter = pageFilter;
        _ofdWorkDir = ofdWorkDir ?? string.Empty;
    }

    public PdfExportSimpleStats GetStats() => _stats;

    public void BeginDocument() { }

    public void EndDocument() { _doc.Flush(); }

    // 解析 CTM 字符串 "a b c d e f" => double[6]
    private static bool TryParseCTM(string? ctmStr, out double a, out double b, out double c, out double d, out double e, out double f)
    {
        a = b = c = d = e = f = 0;
        if (string.IsNullOrWhiteSpace(ctmStr)) return false;
        // 允许逗号或空格
        var parts = ctmStr.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6) return false;
        if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b) &&
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out c) &&
            double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out d) &&
            double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out e) &&
            double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out f))
        {
            return true;
        }
        return false;
    }

    // 应用 CTM 到点 (x,y)
    private static (double X, double Y) ApplyCTM(double x, double y, double a, double b, double c, double d, double e, double f)
        => (a * x + c * y + e, b * x + d * y + f);

    private PdfFont ResolveFont(string? ofdFontName)
    {
        if (string.IsNullOrWhiteSpace(ofdFontName)) return _defaultFont;
        _stats.Fonts.Add(ofdFontName);
        string? mapped = _fontMapper?.Invoke(ofdFontName) ?? ofdFontName;
        if (string.IsNullOrWhiteSpace(mapped)) return _defaultFont;
        if (_fontCache.TryGetValue(mapped, out var cached)) return cached;
        try
        {
            if (_embedFonts && File.Exists(mapped))
            {
                var embedded = PdfFontFactory.CreateFont(mapped, IDENTITY_H);
                _fontCache[mapped] = embedded; _stats.FontsEmbedded++; return embedded;
            }
            // 标准字体映射
            string upper = mapped.ToUpperInvariant();
            if (upper.Contains("HEI") || upper.Contains("黑")) mapped = StandardFonts.HELVETICA;
            else if (upper.Contains("SONG") || upper.Contains("宋")) mapped = StandardFonts.HELVETICA;
            else if (upper.Contains("COURIER")) mapped = StandardFonts.COURIER;
            else if (upper.Contains("TIMES")) mapped = StandardFonts.TIMES_ROMAN;
            var f = PdfFontFactory.CreateFont(mapped);
            _fontCache[mapped] = f; return f;
        }
        catch { return _defaultFont; }
    }

    private static (float asc, float desc) GetFontMetrics(PdfFont font)
    {
        // 优先尝试 FontProgram 度量
        try
        {
            var fp = font.GetFontProgram();
            if (fp != null)
            {
                var metrics = fp.GetFontMetrics();
                if (metrics != null)
                {
                    float asc = metrics.GetTypoAscender() / 1000f;
                    float desc = Math.Abs(metrics.GetTypoDescender()) / 1000f;
                    if (asc > 0 && desc > 0) return (asc, desc);
                }
            }
        }
        catch { }
        return (0.8f, 0.2f);
    }

    // Delta 坐标应用输出（单 TextCode 拆分）
    private void DrawTextWithDelta(PdfFont font, float fontSize, double baseX, double baseY, string text, double[]? deltaX, double[]? deltaY, double a, double b, double c, double d, double e, double f, bool hasCTM, float pageHeight, float baselineAdjust, int pdfPageNum)
    {
        var page = _pdfDoc.GetPage(pdfPageNum);
        var canvas = new PdfCanvas(page);
        double curX = baseX; double curY = baseY;
        for (int i = 0; i < text.Length; i++)
        {
            double x = curX; double y = curY;
            if (hasCTM) (x, y) = ApplyCTM(x, y, a, b, c, d, e, f);
            float pdfY = (float)(pageHeight - y - baselineAdjust);
            canvas.SaveState();
            canvas.BeginText().SetFontAndSize(font, fontSize).MoveText((float)x, pdfY).ShowText(text[i].ToString()).EndText();
            canvas.RestoreState();
            if (deltaX != null && i < deltaX.Length) curX += deltaX[i]; else curX += fontSize * 0.5; // 估算
            if (deltaY != null && i < deltaY.Length) curY += deltaY[i];
        }
    }

    // 解析字符串序列为 double[]
    private static double[]? ParseDoubles(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        var parts = val.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<double>();
        foreach (var p in parts) if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) list.Add(d);
        return list.Count > 0 ? list.ToArray() : null;
    }

    // Path: 处理 Arc (A) 将起点->终点画线占位，CM 矩阵应用
    private static (double a,double b,double c,double d,double e,double f) ConcatMatrix((double a,double b,double c,double d,double e,double f) m, double na, double nb, double nc, double nd, double ne, double nf)
        => (m.a*na + m.c*nb, m.b*na + m.d*nb, m.a*nc + m.c*nd, m.b*nc + m.d*nd, m.a*ne + m.c*nf + m.e, m.b*ne + m.d*nf + m.f);

    private static (double X,double Y) ApplyMatrixToPoint(double x,double y,(double a,double b,double c,double d,double e,double f) m)
        => (m.a * x + m.c * y + m.e, m.b * x + m.d * y + m.f);

    // 恢复辅助方法（图片、颜色、Alpha）
    private string? FindImageFileByResourceId(string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId) || string.IsNullOrWhiteSpace(_ofdWorkDir)) return null;
        try
        {
            var resFiles = Directory.GetFiles(_ofdWorkDir, "*Res.xml", SearchOption.AllDirectories);
            foreach (var file in resFiles)
            {
                try
                {
                    var xdoc = XDocument.Load(file);
                    foreach (var mm in xdoc.Descendants())
                    {
                        var idAttr = mm.Attribute("ID");
                        if (idAttr != null && idAttr.Value == resourceId)
                        {
                            var mediaFileAttr = mm.Attribute("MediaFile")?.Value;
                            string? path = mediaFileAttr;
                            if (string.IsNullOrEmpty(path)) path = mm.Element("MediaFile")?.Value;
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                path = path.Trim().TrimStart('/');
                                var abs = Path.Combine(_ofdWorkDir, path.Replace('/', Path.DirectorySeparatorChar));
                                if (File.Exists(abs)) return abs;
                            }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    private static iText.Kernel.Colors.Color ParseColor(string? val, out float? alpha)
    {
        alpha = null;
        if (string.IsNullOrWhiteSpace(val)) return ColorConstants.BLACK;
        val = val.Trim();
        if (!val.StartsWith("#") || (val.Length != 7 && val.Length != 9)) return ColorConstants.BLACK;
        try
        {
            byte r = Convert.ToByte(val.Substring(1,2),16);
            byte g = Convert.ToByte(val.Substring(3,2),16);
            byte b = Convert.ToByte(val.Substring(5,2),16);
            if (val.Length == 9) { byte a = Convert.ToByte(val.Substring(7,2),16); alpha = a/255f; }
            return new DeviceRgb(r,g,b);
        }
        catch { return ColorConstants.BLACK; }
    }

    private static void ApplyAlpha(PdfCanvas canvas, float? alpha)
    {
        if (alpha.HasValue && alpha.Value < 0.999f)
        {
            var gs = new iText.Kernel.Pdf.Extgstate.PdfExtGState().SetFillOpacity(alpha.Value).SetStrokeOpacity(alpha.Value);
            canvas.SetExtGState(gs);
        }
    }

    public void MakePage(int zeroBasedPageIndex, PageInfo pageInfo)
    {
        int oneBasedPage = zeroBasedPageIndex + 1;
        if (_pageFilter != null && !_pageFilter(oneBasedPage)) return;
        if (zeroBasedPageIndex > 0) _doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        _stats.Pages++;
        var layers = pageInfo.GetAllLayers();
        if (_preserveLayout)
        {
            float pageHeight = (float)pageInfo.Size.Height;
            int pdfPageNum = zeroBasedPageIndex + 1;
            foreach (var layer in layers)
            {
                // ---------- TextObject ----------
                foreach (var textObj in layer.Elements("TextObject"))
                {
                    _stats.TextObjects++;
                    float fontSize = 12f;
                    if (float.TryParse(textObj.Attribute("Size")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fs)) fontSize = fs;
                    double a = 1, b = 0, c = 0, d = 1, e = 0, f = 0;
                    bool hasCTM = TryParseCTM(textObj.Attribute("CTM")?.Value, out a, out b, out c, out d, out e, out f);
                    var ofdFont = textObj.Attribute("FontName")?.Value ?? textObj.Attribute("Font")?.Value;
                    var pdfFont = ResolveFont(ofdFont);
                    var (ascR, descR) = GetFontMetrics(pdfFont);

                    var textCodes = new List<XElement>();
                    foreach (var tc in textObj.Elements("TextCode")) if (!string.IsNullOrWhiteSpace(tc.Value)) textCodes.Add(tc);
                    if (textCodes.Count == 0) continue;
                    textCodes.Sort((x1, x2) => { double xA = 0, xB = 0; double.TryParse(x1.Attribute("X")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out xA); double.TryParse(x2.Attribute("X")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out xB); return xA.CompareTo(xB); });

                    var segments = new List<(double x,double y,string text)>();
                    foreach (var tc in textCodes)
                    {
                        double x = 0, y = 0; double.TryParse(tc.Attribute("X")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x); double.TryParse(tc.Attribute("Y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                        if (segments.Count > 0)
                        {
                            var last = segments[^1];
                            // 粗略按平均字符宽 fontSize*0.5 估算连接
                            if (Math.Abs(y - last.y) < fontSize * 0.3 && x >= last.x)
                            {
                                double expectedNextX = last.x + last.text.Length * fontSize * 0.5;
                                if (Math.Abs(x - expectedNextX) < fontSize * 0.6)
                                { segments[^1] = (last.x, last.y, last.text + tc.Value); continue; }
                            }
                        }
                        segments.Add((x, y, tc.Value));
                    }

                    foreach (var seg in segments)
                    {
                        double x = seg.x; double y = seg.y; string raw = seg.text;
                        double drawX = x, drawY = y;
                        bool transformed = hasCTM && (Math.Abs(b) > 0.0001 || Math.Abs(c) > 0.0001 || Math.Abs(a - 1) > 0.0001 || Math.Abs(d - 1) > 0.0001);
                        float baselineAdjust = fontSize * descR; // descender 部分下移
                        if (transformed)
                        {
                            var page = _pdfDoc.GetPage(pdfPageNum); var canvas = new PdfCanvas(page);
                            canvas.SaveState();
                            double baseE = e + x; double baseF = f + y;
                            double pdfE = baseE; double pdfF = pageHeight - baseF - baselineAdjust;
                            canvas.ConcatMatrix((float)a, (float)b, (float)c, (float)d, (float)pdfE, (float)pdfF);
                            canvas.BeginText().SetFontAndSize(pdfFont, fontSize).ShowText(raw).EndText();
                            canvas.RestoreState();
                            _stats.RotatedText++;
                        }
                        else
                        {
                            if (hasCTM)
                            {
                                (drawX, drawY) = ApplyCTM(x, y, a, b, c, d, e, f);
                            }
                            float pdfY = (float)(pageHeight - drawY - baselineAdjust);
                            var p = new Paragraph(raw).SetFont(pdfFont).SetFontSize(fontSize).SetFixedPosition(pdfPageNum, (float)drawX, pdfY, 5000);
                            _doc.Add(p);
                        }
                        _stats.TextCodes += raw.Length; // 以字符数近似
                    }
                }

                // ---------- ImageObject ----------
                foreach (var imgObj in layer.Elements("ImageObject"))
                {
                    _stats.ImageObjects++;
                    var boundaryAttr = imgObj.Attribute("Boundary")?.Value; if (string.IsNullOrWhiteSpace(boundaryAttr)) continue;
                    var parts = boundaryAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries); if (parts.Length < 4) continue;
                    if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var bx)) continue;
                    if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var by)) continue;
                    if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var bw)) continue;
                    if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var bh)) continue;
                    double ia = 1, ib = 0, ic = 0, idm = 1, ie = 0, iff = 0; bool hasImgCTM = TryParseCTM(imgObj.Attribute("CTM")?.Value, out ia, out ib, out ic, out idm, out ie, out iff);
                    double rx = bx, ry = by; if (hasImgCTM) (rx, ry) = ApplyCTM(bx, by, ia, ib, ic, idm, ie, iff);
                    float pdfYRect = (float)(pageHeight - ry - bh);
                    bool embedded = false;
                    if (_realImageEmbedding)
                    {
                        var resId = imgObj.Attribute("ResourceID")?.Value; var path = FindImageFileByResourceId(resId);
                        if (path != null)
                        {
                            try
                            { var imgData = ImageDataFactory.Create(path); var image = new Image(imgData).SetFixedPosition(pdfPageNum, (float)rx, pdfYRect).ScaleAbsolute((float)bw, (float)bh); _doc.Add(image); _stats.ImagesEmbedded++; embedded = true; }
                            catch { }
                        }
                    }
                    if (!embedded)
                    { var placeholder = new Div().SetBackgroundColor(ColorConstants.LIGHT_GRAY).SetBorder(new iText.Layout.Borders.SolidBorder(ColorConstants.DARK_GRAY, 0.5f)).SetFixedPosition(pdfPageNum, (float)rx, pdfYRect, (float)bw).SetHeight((float)bh); placeholder.Add(new Paragraph("IMG").SetFontSize(6).SetFontColor(ColorConstants.WHITE).SetMargin(0)); _doc.Add(placeholder); }
                }

                // ---------- PathObject ----------
                foreach (var pathObj in layer.Elements("PathObject"))
                {
                    _stats.VectorObjects++;
                    var strokeColorVal = pathObj.Attribute("StrokeColor")?.Value; var fillColorVal = pathObj.Attribute("FillColor")?.Value;
                    float? strokeAlpha, fillAlpha; var strokeColor = ParseColor(strokeColorVal, out strokeAlpha); var fillColor = ParseColor(fillColorVal, out fillAlpha);
                    var abbrElem = pathObj.Element("AbbreviatedData"); if (abbrElem == null) continue;
                    var cmds = OfdrwNet.Core.Graph.PathObj.AbbreviatedData.Parse(abbrElem.Value);
                    var page = _pdfDoc.GetPage(pdfPageNum); var canvas = new PdfCanvas(page);
                    canvas.SaveState(); ApplyAlpha(canvas, fillAlpha); ApplyAlpha(canvas, strokeAlpha);
                    if (double.TryParse(pathObj.Attribute("LineWidth")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lw)) canvas.SetLineWidth((float)lw);
                    var fillRule = pathObj.Attribute("Rule")?.Value; bool evenOdd = fillRule != null && fillRule.Equals("EvenOdd", StringComparison.OrdinalIgnoreCase);
                    // 端帽/连接：使用低层操作符
                    var cap = pathObj.Attribute("Cap")?.Value; if (!string.IsNullOrEmpty(cap)) { int capStyle = cap.ToLowerInvariant() switch { "butt" => 0, "round" => 1, "square" => 2, _ => 0 }; canvas.GetContentStream().GetOutputStream().WriteBytes(System.Text.Encoding.ASCII.GetBytes(capStyle + " J\n")); }
                    var join = pathObj.Attribute("Join")?.Value; if (!string.IsNullOrEmpty(join)) { int joinStyle = join.ToLowerInvariant() switch { "miter" => 0, "round" => 1, "bevel" => 2, _ => 0 }; canvas.GetContentStream().GetOutputStream().WriteBytes(System.Text.Encoding.ASCII.GetBytes(joinStyle + " j\n")); }
                    if (double.TryParse(pathObj.Attribute("MiterLimit")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ml)) canvas.GetContentStream().GetOutputStream().WriteBytes(System.Text.Encoding.ASCII.GetBytes(((float)ml).ToString(CultureInfo.InvariantCulture) + " M\n"));
                    bool moved = false; double curX = 0, curY = 0; var currentMatrix = (a:1d,b:0d,c:0d,d:1d,e:0d,f:0d);
                    foreach (var opt in cmds)
                    {
                        var op = opt.Opt; var vs = opt.Values;
                        switch (op)
                        {
                            case "CM": if (vs.Length >= 6) { currentMatrix = ConcatMatrix(currentMatrix, vs[0], vs[1], vs[2], vs[3], vs[4], vs[5]); } break;
                            case "S": if (vs.Length >= 2) { curX = vs[0]; curY = vs[1]; } break;
                            case "M": if (vs.Length >= 2) { (var tx,var ty) = ApplyMatrixToPoint(vs[0], vs[1], currentMatrix); curX = vs[0]; curY = vs[1]; canvas.MoveTo((float)tx, (float)(pageHeight - ty)); moved = true; } break;
                            case "L": for (int i = 0; i + 1 < vs.Length; i += 2) { var lx = vs[i]; var ly = vs[i + 1]; (var tx,var ty) = ApplyMatrixToPoint(lx, ly, currentMatrix); if (!moved) { (var sx,var sy) = ApplyMatrixToPoint(curX, curY, currentMatrix); canvas.MoveTo((float)sx, (float)(pageHeight - sy)); moved = true; } canvas.LineTo((float)tx, (float)(pageHeight - ty)); curX = lx; curY = ly; } break;
                            case "Q": for (int i = 0; i + 3 < vs.Length; i += 4) { var x1 = vs[i]; var y1 = vs[i + 1]; var x2 = vs[i + 2]; var y2 = vs[i + 3]; (var tx1,var ty1) = ApplyMatrixToPoint(x1,y1,currentMatrix); (var tx2,var ty2) = ApplyMatrixToPoint(x2,y2,currentMatrix); canvas.CurveTo((float)tx1,(float)(pageHeight - ty1),(float)tx2,(float)(pageHeight - ty2),(float)tx2,(float)(pageHeight - ty2)); curX = x2; curY = y2; } break;
                            case "B": for (int i = 0; i + 5 < vs.Length; i += 6) { var x1c = vs[i]; var y1c = vs[i + 1]; var x2c = vs[i + 2]; var y2c = vs[i + 3]; var x3c = vs[i + 4]; var y3c = vs[i + 5]; (var tx1,var ty1) = ApplyMatrixToPoint(x1c,y1c,currentMatrix); (var tx2,var ty2) = ApplyMatrixToPoint(x2c,y2c,currentMatrix); (var tx3,var ty3) = ApplyMatrixToPoint(x3c,y3c,currentMatrix); canvas.CurveTo((float)tx1,(float)(pageHeight - ty1),(float)tx2,(float)(pageHeight - ty2),(float)tx3,(float)(pageHeight - ty3)); curX = x3c; curY = y3c; } break;
                            case "A": if (vs.Length >= 7) { var ex = vs[^2]; var ey = vs[^1]; (var tx,var ty) = ApplyMatrixToPoint(ex,ey,currentMatrix); if (!moved) { (var sx,var sy) = ApplyMatrixToPoint(curX,curY,currentMatrix); canvas.MoveTo((float)sx,(float)(pageHeight - sy)); moved = true; } canvas.LineTo((float)tx,(float)(pageHeight - ty)); curX = ex; curY = ey; } break;
                            case "C": canvas.ClosePath(); break;
                        }
                    }
                    if (fillColorVal != null) canvas.SetFillColor(fillColor); if (strokeColorVal != null) canvas.SetStrokeColor(strokeColor);
                    bool doFill = fillColorVal != null; bool doStroke = strokeColorVal != null;
                    if (doFill && doStroke) { if (evenOdd) canvas.EoFillStroke(); else canvas.FillStroke(); }
                    else if (doFill) { if (evenOdd) canvas.EoFill(); else canvas.Fill(); }
                    else if (doStroke) canvas.Stroke();
                    canvas.RestoreState();
                }
            }
        }
        else
        {
            // 流式输出：添加页标题 + 按层/对象顺序写入
            _doc.Add(new Paragraph($"Page {zeroBasedPageIndex + 1}").SetFont(_defaultFont).SetFontSize(10).SetFontColor(ColorConstants.GRAY));
            foreach (var layer in layers)
            {
                var textObjects = layer.Elements("TextObject");
                foreach (var textObj in textObjects)
                {
                    _stats.TextObjects++;
                    var fontSizeStr = textObj.Attribute("Size")?.Value;
                    float fontSize = 12f;
                    float.TryParse(fontSizeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out fontSize);
                    var ofdFont = textObj.Attribute("FontName")?.Value ?? textObj.Attribute("Font")?.Value;
                    var pdfFont = ResolveFont(ofdFont);

                    foreach (var textCode in textObj.Elements("TextCode"))
                    {
                        var txt = textCode.Value;
                        if (string.IsNullOrWhiteSpace(txt)) continue;
                        _stats.TextCodes++;
                        var para = new Paragraph(txt).SetFont(pdfFont).SetFontSize(fontSize);
                        _doc.Add(para);
                    }
                }
                foreach (var imgObj in layer.Elements("ImageObject")) _stats.ImageObjects++;
            }
        }
    }

    public void Dispose() { _doc?.Close(); }
}

/// <summary>
/// PDF导出器（重构：引入页面生成器 + 统计）
/// </summary>
public class PDFExporter : OFDExporterBase
{
    private readonly float _dpi;
    private string? _pdfOutputPath;
    private readonly bool _preserveLayout;
    private readonly IProgress<(int done, int total)>? _progress;
    private readonly string? _statsJsonPath;
    private readonly Func<string, string?>? _fontMapper;
    private readonly bool _embedFonts;
    private readonly bool _realImageEmbedding;
    private readonly Func<int,bool>? _pageFilter; // 新增

    public PDFExporter(string ofdPath, string pdfOutputPath, float dpi = 150f, bool preserveLayout = false, IProgress<(int done, int total)>? progress = null,
        string? statsJsonPath = null, Func<string, string?>? fontMapper = null, bool embedFonts = false, bool realImageEmbedding = false, Func<int,bool>? pageFilter = null)
        : base(ofdPath, Path.GetDirectoryName(pdfOutputPath) ?? ".")
    {
        _pdfOutputPath = pdfOutputPath;
        _dpi = Math.Max(72f, dpi);
        _preserveLayout = preserveLayout;
        _progress = progress;
        _statsJsonPath = statsJsonPath;
        _fontMapper = fontMapper;
        _embedFonts = embedFonts;
        _realImageEmbedding = realImageEmbedding;
        _pageFilter = pageFilter;
        _outputPaths.Add(_pdfOutputPath);
    }

    /// <summary>
    /// 导出所有页面为单个PDF文件
    /// </summary>
    public override async Task ExportAsync(CancellationToken cancellationToken = default)
    {
        Initialize();
        var pageCount = _reader!.GetNumberOfPages();
        await ConvertToPdf(0, pageCount - 1, cancellationToken);
    }

    /// <summary>
    /// 导出指定页面范围为PDF
    /// </summary>
    public override async Task ExportAsync(int startPageNum, int endPageNum, CancellationToken cancellationToken = default)
    {
        Initialize();
        ValidatePageRange(startPageNum, endPageNum);
        await ConvertToPdf(startPageNum, endPageNum, cancellationToken);
    }

    /// <summary>
    /// 导出单个页面（实际上创建只包含该页的PDF）
    /// </summary>
    protected override async Task ExportPageAsync(int pageNum, CancellationToken cancellationToken)
    {
        await ConvertToPdf(pageNum, pageNum, cancellationToken);
    }

    private async Task ConvertToPdf(int startPageNum, int endPageNum, CancellationToken cancellationToken)
    {
        try
        {
            using var pdfWriter = new PdfWriter(_pdfOutputPath!);
            using var pdfDoc = new PdfDocument(pdfWriter);
            using IPdfPageMaker maker = new ITextPageMaker(pdfDoc, _preserveLayout, _fontMapper, _embedFonts, _realImageEmbedding, _pageFilter, _reader!.GetWorkDir());
            maker.BeginDocument();
            var pageIndices = new List<int>();
            for (int p = startPageNum; p <= endPageNum; p++)
            {
                if (_pageFilter == null || _pageFilter(p + 1)) pageIndices.Add(p);
            }
            int total = pageIndices.Count == 0 ? (endPageNum - startPageNum + 1) : pageIndices.Count;
            int done = 0;
            foreach (var p in pageIndices)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageInfo = _reader!.GetPageInfo(p + 1);
                maker.MakePage(p - startPageNum, pageInfo);
                done++;
                _progress?.Report((done, total));
            }
            maker.EndDocument();
            if (!string.IsNullOrWhiteSpace(_statsJsonPath))
            {
                try
                {
                    var stats = maker.GetStats();
                    var json = JsonSerializer.Serialize(new
                    {
                        stats.Pages,
                        stats.TextObjects,
                        stats.TextCodes,
                        stats.ImageObjects,
                        stats.RotatedText,
                        stats.VectorObjects,
                        stats.ImagesEmbedded,
                        stats.FontsEmbedded,
                        FontNames = stats.Fonts,
                        FontCount = stats.Fonts.Count
                    }, new JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(_statsJsonPath!, json);
                }
                catch { }
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new GeneralConvertException($"PDF导出失败: {ex.Message}", ex);
        }
    }

    private async Task ConvertViaVectorToPdf(int startPageNum, int endPageNum, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("矢量PDF转换功能尚未实现");
    }
}