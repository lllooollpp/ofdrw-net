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

                            // 将 mm 转为像素 (独立缩放)
                            float Px(float mm, bool isY) => (float)(mm * (isY ? scaleY : scaleX));
                            var pxX = Px(x, false);
                            var pxYTop = Px(y, true);
                            var pxWidth = Math.Max(1f, Px(w, false));
                            var pxHeight = Math.Max(1f, Px(h, true));
                            var pxFontSize = (float)(fontSize * (scaleY));

                            // 修复坐标计算：确保文本在可见区域内
                            // 如果原始Y坐标为负，使用boundary的top作为基准
                            // 重新计算基线与文本顶部：若 boundary 已是文本框顶部，则直接使用 y
                            var baselinePixel = pxYTop + Px(Math.Max(0, baselineOffset), true);
                            float textTopPixel;
                            if (baselineOffset > 0 && baselineOffset < h * 2)
                            {
                                // baselineOffset 合理时：尝试向上回推 (估算基线到顶部距离 ~0.8*fontSize)
                                textTopPixel = baselinePixel - pxFontSize * 0.8f;
                            }
                            else
                            {
                                textTopPixel = pxYTop; // 无有效基线信息直接用 boundary 顶部
                            }
                            if (textTopPixel < 0) textTopPixel = 0;
                            if (textTopPixel > 50000) textTopPixel = pxYTop; // 异常回退

                            var id = to.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N");

                            var txtObj = new TextObject
                            {
                                Id = id,
                                Text = text,
                                FontSize = pxFontSize,
                                Font = new FontInfo { Name = fontName, Size = pxFontSize },
                                Boundary = new RectangleF(pxX, textTopPixel, pxWidth, pxHeight),
                                Layout = new TextLayout
                                {
                                    X = pxX,
                                    Y = textTopPixel,
                                    Width = pxWidth,
                                    Height = pxHeight,
                                    LineHeight = 1.0f
                                },
                                ZIndex = z++,
                                ZOrder = z,
                                Visible = true
                            };

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

                            // 如果没有Path数据，创建一个简单的矩形
                            if (string.IsNullOrWhiteSpace(pathData) && w > 0 && h > 0)
                            {
                                pathData = $"M {x} {y} L {x + w} {y} L {x + w} {y + h} L {x} {y + h} Z";
                            }

                            if (!string.IsNullOrWhiteSpace(pathData))
                            {
                                float Px(float mm, bool isY) => (float)(mm * (isY ? scaleY : scaleX));
                                var pxX = Px(x, false);
                                var pxY = Px(y, true);
                                var pxW = Math.Max(1f, Px(w, false));
                                var pxH = Math.Max(1f, Px(h, true));

                                var vectorObj = new VectorObject
                                {
                                    Id = po.Attribute("ID")?.Value ?? Guid.NewGuid().ToString("N"),
                                    PathData = pathData,
                                    Boundary = new System.Drawing.RectangleF(pxX, pxY, pxW, pxH),
                                    ZIndex = z++,
                                    ZOrder = z,
                                    VectorType = VectorType.Path,
                                    Visible = true
                                };
                                result.Add(vectorObj);
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
