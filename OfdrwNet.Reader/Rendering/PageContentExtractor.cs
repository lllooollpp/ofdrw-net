using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 极简页面内容提取器（临时实现）
    /// 仅解析 TextObject -> TextCode ，后续可扩展 ImageObject / Path 等。
    /// </summary>
    public static class PageContentExtractor
    {
        // 简单诊断日志缓存（最多100条）
        private static readonly Queue<string> _diag = new Queue<string>();
        private static bool IsUnified()
        {
            try
            {
                var t = Type.GetType("OfdrwNet.Reader.Rendering.RenderingConfig");
                if (t != null)
                {
                    var p = t.GetProperty("UnifiedScalingMode");
                    if (p != null) return (bool)p.GetValue(null)!;
                }
            }
            catch { }
            return false;
        }
    /// <summary>
    /// 最近一次页面内容提取的诊断日志（最多100条）
    /// </summary>
    public static IReadOnlyCollection<string> Diagnostics => _diag.ToList().AsReadOnly();
        private static void Log(string msg)
        {
            if (_diag.Count > 100) _diag.Dequeue();
            _diag.Enqueue($"[{DateTime.UtcNow:HH:mm:ss}] {msg}");
        }
        /// <summary>
        /// 从 PageInfo 中提取可渲染对象集合
        /// </summary>
        public static List<RenderObject> ExtractRenderObjects(PageInfo pageInfo, double scaleX = 1.0, double scaleY = 1.0)
        {
            var result = new List<RenderObject>();
            try
            {
                if (pageInfo == null || pageInfo.Obj == null)
                    return result;

                // 获取所有图层（含模板）
                var layers = pageInfo.GetAllLayers();
                Log($"Page {pageInfo.Index} initial layers: {layers.Count}");
                if (layers.Count == 0)
                {
                    // 兜底1：全局扫描 Layer
                    layers = pageInfo.Obj
                        .Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "Layer", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    Log($"Page {pageInfo.Index} fallback Layer scan count: {layers.Count}");
                }
                if (layers.Count == 0)
                {
                    // 兜底2：没有任何 Layer，OFD 有时直接在 Content 下放 TextObject/ImageObject
                    // 将所有 Content 元素自身作为“伪层”处理
                    layers = pageInfo.Obj
                        .Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "Content", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    Log($"Page {pageInfo.Index} pseudo Content layers: {layers.Count}");
                }
                int z = 0;
                foreach (var layer in layers)
                {
                    // 查找文本对象（无视命名空间）
                    var textObjects = layer
                        .Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "TextObject", StringComparison.OrdinalIgnoreCase));
                    int textCountLayer = 0;

                    foreach (var to in textObjects)
                    {
                        try
                        {
                            var text = string.Join("", to
                                .Descendants()
                                .Where(e => string.Equals(e.Name.LocalName, "TextCode", StringComparison.OrdinalIgnoreCase))
                                .Select(e => (e.Value ?? string.Empty)));

                            if (string.IsNullOrWhiteSpace(text))
                                continue; // 空文本忽略

                            // 解析 Boundary 属性（格式: x y w h，以空格或逗号分隔）
                            var boundaryAttr = to.Attribute("Boundary")?.Value;
                            // OFD 坐标单位：毫米
                            float x = 50, y = 50, w = Math.Max(10, text.Length * 5), h = 10; // 默认值（mm）
                            if (!string.IsNullOrWhiteSpace(boundaryAttr))
                            {
                                var parts = boundaryAttr
                                    .Replace(',', ' ')
                                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 &&
                                    TryParseFloat(parts[0], out var fx) &&
                                    TryParseFloat(parts[1], out var fy) &&
                                    TryParseFloat(parts[2], out var fw) &&
                                    TryParseFloat(parts[3], out var fh))
                                {
                                    x = fx; y = fy; w = Math.Max(1, fw); h = Math.Max(1, fh);
                                }
                            }

                            // 字号（若有 Size / FontSize 或者 Attribute Size）
                            float fontSize = 12f;
                            var sizeAttr = to.Attribute("Size")?.Value ?? to.Attribute("FontSize")?.Value;
                            if (!string.IsNullOrWhiteSpace(sizeAttr) && TryParseFloat(sizeAttr, out var fs))
                            {
                                fontSize = fs;
                            }

                            // 字体名（Font / FontName）
                            var fontName = to.Attribute("Font")?.Value ?? to.Attribute("FontName")?.Value ?? "SimSun";
                            // 如果是纯数字（资源ID），暂时映射为常见中文字体
                            if (!string.IsNullOrEmpty(fontName) && fontName.All(char.IsDigit))
                            {
                                fontName = "SimSun"; // 简单映射，可后续接入资源管理器真正解析
                            }

                            // TextCode 里的基线偏移：选择第一个 TextCode 做定位
                            float baselineOffset = 0f; // 相对于 boundary.Top 的基线 (mm)
                            var firstTextCode = to
                                .Descendants()
                                .FirstOrDefault(e => e.Name.LocalName.Equals("TextCode", StringComparison.OrdinalIgnoreCase));
                            if (firstTextCode != null)
                            {
                                if (TryParseFloat(firstTextCode.Attribute("Y")?.Value, out var yBase))
                                {
                                    baselineOffset = yBase; // OFD 中 Y 通常是基线到 boundary.Top 的距离（mm）
                                }
                            }

                            bool unified = IsUnified();

                            // unified 模式：保留 mm；非 unified：转换为像素
                            float Px(float mm, bool isY) => (float)(mm * (isY ? scaleY : scaleX));

                            float finalX, finalYTop, finalWidth, finalHeight, finalFontSize, finalBaselineOffset;

                            if (!unified)
                            {
                                // 原像素路径（方案B沿用）
                                var pxX = Px(x, false);
                                var pxYTop = Px(y, true);
                                var pxWidth = Math.Max(1f, Px(w, false));
                                var rawFontPx = (float)(fontSize * scaleY);
                                var boundaryHeightPx = Px(h, true);
                                float pxFontSize;
                                if (boundaryHeightPx > 0.1f && boundaryHeightPx < rawFontPx * 2)
                                    pxFontSize = boundaryHeightPx * 0.85f;
                                else
                                    pxFontSize = Math.Min(rawFontPx, 180f);
                                var pxHeight = Math.Max(pxFontSize * 1.15f, boundaryHeightPx);
                                var baselineOffsetPx = Px(Math.Max(0, baselineOffset), true);
                                float baselinePixel = (baselineOffsetPx > 0 && baselineOffsetPx < pxHeight * 3)
                                    ? pxYTop + baselineOffsetPx
                                    : pxYTop + pxFontSize * 0.8f;
                                float textTopPixel = baselinePixel - pxFontSize * 0.82f;
                                if (textTopPixel < 0) textTopPixel = 0;

                                finalX = pxX;
                                finalYTop = textTopPixel;
                                finalWidth = pxWidth;
                                finalHeight = Math.Max(pxHeight, 1f);
                                finalFontSize = pxFontSize; // 像素字号
                                finalBaselineOffset = baselinePixel - textTopPixel; // 像素
                            }
                            else
                            {
                                // unified：全部保留 mm，渲染阶段统一 * Ppm * ScaleFactor
                                finalX = x;
                                finalYTop = y; // 先用 boundary.Top，基线由 BaselineOffset 辅助
                                finalWidth = Math.Max(0.1f, w);
                                finalHeight = Math.Max(0.1f, h);

                                // 估算字体大小：取 boundary 高度 *0.85 （mm） 或 使用 fontSize mm（OFD 字号单位也是 mm）
                                // 若 boundary 高度异常，则 fallback 字号
                                float fontSizeMm;
                                if (h > 0.1f && h < fontSize * 2)
                                    fontSizeMm = h * 0.85f;
                                else
                                    fontSizeMm = fontSize;
                                // 限制过大字号（例如错误数据）
                                fontSizeMm = Math.Min(fontSizeMm, 50f); // 50mm ~ 1968px @ 100ppm
                                finalFontSize = fontSizeMm; // mm 单位，TextRenderer 中再转像素

                                // 基线偏移 mm -> 暂用 baselineOffset；若无则用 ascent 近似（0.8*fontSizeMm）
                                float baselineOffsetMm = baselineOffset > 0 ? baselineOffset : fontSizeMm * 0.8f;
                                finalBaselineOffset = baselineOffsetMm; // 保持 mm
                            }

                            var id = to.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N");

                            // 解析 CTM（如果存在）
                            System.Drawing.Drawing2D.Matrix? ctmMatrix = null;
                            var ctmAttr = to.Attribute("CTM")?.Value;
                            if (!string.IsNullOrWhiteSpace(ctmAttr))
                            {
                                var nums = ctmAttr.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (nums.Length == 6 &&
                                    TryParseFloat(nums[0], out var a) &&
                                    TryParseFloat(nums[1], out var b) &&
                                    TryParseFloat(nums[2], out var c) &&
                                    TryParseFloat(nums[3], out var d) &&
                                    TryParseFloat(nums[4], out var e) &&
                                    TryParseFloat(nums[5], out var f))
                                {
                                    // OFD CTM 映射到 GDI Matrix(a,b,c,d,e,f)
                                    ctmMatrix = new System.Drawing.Drawing2D.Matrix(a, b, c, d, e * (float)scaleX, f * (float)scaleY);
                                }
                            }

                            var txtObj = new TextObject
                            {
                                Id = id,
                                Text = text,
                                FontSize = finalFontSize,
                                Font = new FontInfo { Name = fontName, Size = finalFontSize },
                                Color = new ColorInfo { R = 0, G = 0, B = 0, A = 255 }, // 默认黑色
                                Boundary = new RectangleF(finalX, finalYTop, finalWidth, finalHeight),
                                Layout = new TextLayout
                                {
                                    X = finalX,
                                    Y = finalYTop,
                                    Width = finalWidth,
                                    Height = finalHeight,
                                    LineHeight = 1.0f,
                                    BaselineOffset = finalBaselineOffset
                                },
                                ZIndex = z++,
                                ZOrder = z,
                                Visible = true,
                                OriginalCTM = ctmMatrix?.Clone(),
                                CTM = ctmMatrix // 先赋值，后面再基于策略调整
                            };

                            // 策略：如果处于 legacy(非 unified) 模式，并且我们已经把 boundary / fontSize 转成像素，
                            // 还继续应用 CTM 会造成重复缩放。此类场景将 CTM 标记为内部坐标已融合并清空用于渲染的 CTM。
                            if (!unified && txtObj.CTM != null)
                            {
                                txtObj.CtmIsInternalGlyph = true;
                                txtObj.CTM?.Dispose();
                                txtObj.CTM = null; // 避免再次 MultiplyTransform
                            }
                            // Unified 模式下：保留 CTM 让渲染阶段叠乘

                            result.Add(txtObj);
                            textCountLayer++;
                        }
                        catch
                        {
                            // 单个对象解析失败忽略（避免影响整体）
                        }
                    }
                    Log($"Page {pageInfo.Index} layer parsed TextObjects: {textCountLayer}");

                    // ===== ImageObject 解析 =====
                    var imageObjects = layer
                        .Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "ImageObject", StringComparison.OrdinalIgnoreCase));
                    int imgCountLayer = 0;
                    foreach (var io in imageObjects)
                    {
                        try
                        {
                            var resId = io.Attribute("ResourceID")?.Value;
                            if (string.IsNullOrEmpty(resId)) continue;

                            var boundaryAttr = io.Attribute("Boundary")?.Value;
                            float x = 0, y = 0, w = 0, h = 0;
                            if (!string.IsNullOrWhiteSpace(boundaryAttr))
                            {
                                var parts = boundaryAttr.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 &&
                                    TryParseFloat(parts[0], out var fx) &&
                                    TryParseFloat(parts[1], out var fy) &&
                                    TryParseFloat(parts[2], out var fw) &&
                                    TryParseFloat(parts[3], out var fh))
                                {
                                    x = fx; y = fy; w = fw; h = fh;
                                }
                            }

                            // CTM 解析（a b c d e f）
                            System.Drawing.Drawing2D.Matrix? ctmMatrix = null;
                            var ctmAttr = io.Attribute("CTM")?.Value;
                            if (!string.IsNullOrWhiteSpace(ctmAttr))
                            {
                                var nums = ctmAttr.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (nums.Length == 6 &&
                                    TryParseFloat(nums[0], out var a) &&
                                    TryParseFloat(nums[1], out var b) &&
                                    TryParseFloat(nums[2], out var c) &&
                                    TryParseFloat(nums[3], out var d) &&
                                    TryParseFloat(nums[4], out var e) &&
                                    TryParseFloat(nums[5], out var f))
                                {
                                    // OFD CTM: [ a c e ; b d f ; 0 0 1 ]  -> GDI+ Matrix(m11=a, m12=b, m21=c, m22=d, dx=e, dy=f)
                                    ctmMatrix = new System.Drawing.Drawing2D.Matrix(a, b, c, d, e * (float)scaleX, f * (float)scaleY);
                                }
                            }

                            bool unifiedImg = IsUnified();
                            if (!unifiedImg)
                            {
                                float Px(float mm, bool isY) => (float)(mm * (isY ? scaleY : scaleX));
                                var pxX = Px(x, false);
                                var pxY = Px(y, true);
                                var pxW = Math.Max(1f, Px(w, false));
                                var pxH = Math.Max(1f, Px(h, true));
                                var imgObj = new ImageObject
                                {
                                    Id = io.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N"),
                                    ResourceId = resId,
                                    Boundary = new System.Drawing.RectangleF(pxX, pxY, pxW, pxH),
                                    ZIndex = z++,
                                    ZOrder = z,
                                    CTM = ctmMatrix
                                };
                                result.Add(imgObj);
                            }
                            else
                            {
                                // 保留 mm
                                var imgObj = new ImageObject
                                {
                                    Id = io.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N"),
                                    ResourceId = resId,
                                    Boundary = new System.Drawing.RectangleF(x, y, Math.Max(0.1f, w), Math.Max(0.1f, h)),
                                    ZIndex = z++,
                                    ZOrder = z,
                                    CTM = ctmMatrix
                                };
                                result.Add(imgObj);
                            }
                            imgCountLayer++;
                        }
                        catch
                        {
                            // 忽略单个失败
                        }
                    }
                    Log($"Page {pageInfo.Index} layer parsed ImageObjects: {imgCountLayer}");

                    // ===== PathObject / 矢量对象解析 =====
                    var pathObjects = layer
                        .Descendants()
                        .Where(e => string.Equals(e.Name.LocalName, "PathObject", StringComparison.OrdinalIgnoreCase));
                    int pathCountLayer = 0;
                    foreach (var po in pathObjects)
                    {
                        try
                        {
                            var boundaryAttr = po.Attribute("Boundary")?.Value;
                            float x = 0, y = 0, w = 0, h = 0;
                            if (!string.IsNullOrWhiteSpace(boundaryAttr))
                            {
                                var parts = boundaryAttr.Replace(',', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 &&
                                    TryParseFloat(parts[0], out var fx) &&
                                    TryParseFloat(parts[1], out var fy) &&
                                    TryParseFloat(parts[2], out var fw) &&
                                    TryParseFloat(parts[3], out var fh))
                                {
                                    x = fx; y = fy; w = fw; h = fh;
                                }
                            }

                            // 查找 Path 子元素
                            var pathElement = po.Descendants()
                                .FirstOrDefault(e => string.Equals(e.Name.LocalName, "Path", StringComparison.OrdinalIgnoreCase));

                            string pathData = "";
                            if (pathElement != null)
                            {
                                // 查找 AbbreviatedData 属性或元素内容
                                pathData = pathElement.Attribute("AbbreviatedData")?.Value ?? pathElement.Value ?? "";
                            }
                            // 兼容直接 AbbreviatedData 元素（无 <Path> 包裹）
                            if (string.IsNullOrWhiteSpace(pathData))
                            {
                                var abbrElem = po.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "AbbreviatedData", StringComparison.OrdinalIgnoreCase));
                                if (abbrElem != null)
                                {
                                    pathData = abbrElem.Value ?? "";
                                }
                            }

                            // 如果没有Path数据，创建一个简单的矩形
                            if (string.IsNullOrWhiteSpace(pathData) && w > 0 && h > 0)
                            {
                                pathData = $"M {x} {y} L {x + w} {y} L {x + w} {y + h} L {x} {y + h} Z";
                            }
                            // 解析 PathObject 绘制属性
                            bool strokeEnabled = true; // 默认描边
                            bool fillEnabled = false;  // 默认不填充
                            float lineWidthMm = 0.2f;  // 默认细线
                            var strokeColor = ParseColor(po.Attribute("StrokeColor")?.Value) ?? ParseColor(po.Attribute("StrokeColour")?.Value);
                            var fillColor = ParseColor(po.Attribute("FillColor")?.Value) ?? ParseColor(po.Attribute("FillColour")?.Value);
                            var strokeAttr = po.Attribute("Stroke")?.Value;
                            if (!string.IsNullOrWhiteSpace(strokeAttr))
                            {
                                if (bool.TryParse(strokeAttr, out var b)) strokeEnabled = b; else strokeEnabled = strokeAttr != "false" && strokeAttr != "0";
                            }
                            var fillAttr = po.Attribute("Fill")?.Value;
                            if (!string.IsNullOrWhiteSpace(fillAttr))
                            {
                                if (bool.TryParse(fillAttr, out var b)) fillEnabled = b; else fillEnabled = fillAttr == "true" || fillAttr == "1";
                            }
                            if (TryParseFloat(po.Attribute("LineWidth")?.Value, out var lw) && lw > 0) lineWidthMm = lw;

                            bool unifiedVec = IsUnified();
                            float lineWidthFinal = lineWidthMm;
                            if (!unifiedVec)
                            {
                                lineWidthFinal = (float)(lineWidthMm * (scaleX + scaleY) / 2.0);
                            }

                            // 识别极细长矩形为线：增加纵横比约束，避免误把有大 StrokeWidth 的形状当作线
                            bool thinAsLine = false;
                            if (w > 0 && h > 0)
                            {
                                float minSide = Math.Min(w, h);      // mm
                                float maxSide = Math.Max(w, h);      // mm
                                float aspect = maxSide / Math.Max(minSide, 0.01f);
                                // 条件：厚度 < 0.6mm 且 长度 > 1.2mm 且 长宽比 >= 8 视为线
                                if (minSide < 0.6f && maxSide > 1.2f && aspect >= 8f)
                                {
                                    thinAsLine = true;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(pathData) || thinAsLine)
                            {
                                float Px(float mm, bool isY) => (float)(mm * (isY ? scaleY : scaleX));
                                var bX = unifiedVec ? x : Px(x, false);
                                var bY = unifiedVec ? y : Px(y, true);
                                var bW = unifiedVec ? Math.Max(0.1f, w) : Math.Max(1f, Px(w, false));
                                var bH = unifiedVec ? Math.Max(0.1f, h) : Math.Max(1f, Px(h, true));
                                var id = po.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N");
                                var vectorObj = new VectorObject
                                {
                                    Id = id,
                                    Boundary = new RectangleF(bX, bY, bW, bH),
                                    ZIndex = z++,
                                    ZOrder = z,
                                    Visible = true
                                };
                                if (thinAsLine)
                                {
                                    vectorObj.VectorType = VectorType.Line;
                                    vectorObj.PathData = null;
                                    if (bW >= bH)
                                    {
                                        vectorObj.Points = new List<PointF> { new PointF(bX, bY + bH / 2), new PointF(bX + bW, bY + bH / 2) };
                                    }
                                    else
                                    {
                                        vectorObj.Points = new List<PointF> { new PointF(bX + bW / 2, bY), new PointF(bX + bW / 2, bY + bH) };
                                    }
                                }
                                else
                                {
                                    vectorObj.VectorType = VectorType.Path;
                                    vectorObj.PathData = pathData;
                                }
                                if (strokeEnabled)
                                {
                                    // 如果这是识别出的“线”且声明的 LineWidth 远大于实际厚度，则按实际厚度重置
                                    if (thinAsLine)
                                    {
                                        float thicknessPx = unifiedVec ? Math.Min(w, h) : Math.Min(vectorObj.Boundary.Width, vectorObj.Boundary.Height);
                                        if (thicknessPx <= 0) thicknessPx = 0.5f;
                                        // 若推导的描边宽度超过实际厚度 1.5 倍，说明原属性更可能用于其它形状情景，改用真实厚度
                                        if (lineWidthFinal > thicknessPx * 1.5f)
                                        {
                                            float old = lineWidthFinal;
                                            lineWidthFinal = thicknessPx;
                                            Log($"Page {pageInfo.Index} line id={id} stroke adjusted {old:0.###}-> {lineWidthFinal:0.###} (thickness)");
                                        }
                                        // 极端异常：如果属性写了几十上百 mm（转换后巨大），做硬性上限
                                        if (lineWidthFinal > 5f * thicknessPx && thicknessPx < 5f)
                                        {
                                            float old2 = lineWidthFinal;
                                            lineWidthFinal = Math.Min(old2, thicknessPx * 2f);
                                            Log($"Page {pageInfo.Index} line id={id} stroke clamped {old2:0.###}-> {lineWidthFinal:0.###} (hard clamp)");
                                        }
                                    }
                                    vectorObj.StrokeStyle = new StrokeStyle { Width = lineWidthFinal, Color = strokeColor ?? new ColorInfo { R = 0, G = 0, B = 0, A = 255 } };
                                }
                                if (fillEnabled)
                                {
                                    vectorObj.FillStyle = new FillStyle { Color = fillColor ?? new ColorInfo { R = 0, G = 0, B = 0, A = 64 } };
                                }
                                result.Add(vectorObj);
                                Log($"Page {pageInfo.Index} add Vector id={id} type={vectorObj.VectorType} stroke={(strokeEnabled ? lineWidthFinal.ToString("0.###") : "none")} pathLen={(vectorObj.PathData?.Length ?? 0)} boundary=({bX:0.##},{bY:0.##},{bW:0.##},{bH:0.##}) thin={thinAsLine}");
                                pathCountLayer++;
                            }
                        }
                        catch
                        {
                            // 忽略单个失败
                        }
                    }
                    Log($"Page {pageInfo.Index} layer parsed PathObjects: {pathCountLayer}");
                }
                Log($"Page {pageInfo.Index} total objects: {result.Count}");
            }
            catch
            {
                // 整体失败则返回目前已成功解析的部分
                Log($"Page {pageInfo?.Index} extraction exception; partial count={result.Count}");
            }
            return result;
        }

        private static bool TryParseFloat(string? s, out float value)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   float.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// 解析颜色字符串
        /// </summary>
        private static ColorInfo? ParseColor(string? colorStr)
        {
            if (string.IsNullOrWhiteSpace(colorStr))
                return null;

            try
            {
                // 支持RGB格式："#RRGGBB"或"RRGGBB"
                if (colorStr.StartsWith("#"))
                    colorStr = colorStr.Substring(1);

                if (colorStr.Length == 6 && int.TryParse(colorStr, NumberStyles.HexNumber, null, out var rgb))
                {
                    return new ColorInfo
                    {
                        R = (byte)((rgb >> 16) & 0xFF),
                        G = (byte)((rgb >> 8) & 0xFF),
                        B = (byte)(rgb & 0xFF),
                        A = 255
                    };
                }

                // 支持RGB逗号分隔："255,0,0"
                var parts = colorStr.Split(',', ' ');
                if (parts.Length >= 3 &&
                    byte.TryParse(parts[0], out var r) &&
                    byte.TryParse(parts[1], out var g) &&
                    byte.TryParse(parts[2], out var b))
                {
                    byte a = 255;
                    if (parts.Length >= 4)
                        byte.TryParse(parts[3], out a);

                    return new ColorInfo { R = r, G = g, B = b, A = a };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
