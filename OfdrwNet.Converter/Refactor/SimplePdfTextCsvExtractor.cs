// using iText.Kernel.Font;
// using iText.Kernel.Geom;
// using iText.Kernel.Pdf;
// using iText.Kernel.Pdf.Canvas.Parser;
// using iText.Kernel.Pdf.Canvas.Parser.Data;
// using iText.Kernel.Pdf.Canvas.Parser.Listener;
// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Linq;
// using System.Text;

// namespace OfdrwNet.Converter.Refactor
// {
//     // 精简版：将 PDF 文本按连续字串分块并导出为 CSV（用于调试/比对）
//     internal class SimplePdfTextCsvExtractor
//     {
//         private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

//         private class OFDMapper
//         {
//             private Dictionary<PdfFont, int> fontMap = new Dictionary<PdfFont, int>(ReferenceEqualityComparer<PdfFont>.Default);
//             private int nextFontId = 1;

//             public int EnsureFontId(PdfFont font)
//             {
//                 if (font == null) return 0;
//                 if (!fontMap.TryGetValue(font, out int id))
//                 {
//                     id = nextFontId++;
//                     fontMap[font] = id;
//                 }
//                 return id;
//             }

//             public TextObjectRecord Map(IReadOnlyList<TextRenderInfo> renderInfos, float pageHeight, int recordId, IReadOnlyList<float>? deltaX = null)
//             {
//                 // 简化：使用 ascent bbox 的并集作为 boundary，textCodeY 使用 baseline 转换到 OFD 坐标系
//                 float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
//                 foreach (var ri in renderInfos)
//                 {
//                     var bbox = ri.GetAscentLine().GetBoundingRectangle();
//                     minX = Math.Min(minX, bbox.GetLeft());
//                     maxX = Math.Max(maxX, bbox.GetRight());
//                     minY = Math.Min(minY, bbox.GetBottom());
//                     maxY = Math.Max(maxY, bbox.GetTop());
//                 }

//                 var first = renderInfos.First();
//                 var font = first.GetFont();
//                 int fontId = EnsureFontId(font);
//                 float fontSize = first.GetFontSize();
//                 var ctm = first.GetGraphicsState().GetCtm();
//                 float[] c = new float[6];
//                 for (int i = 0; i < 6; i++) c[i] = ctm.Get(i);

//                 // textCode 原点转为 OFD: x 保持 0, y = baseline - pageHeight
//                 float baselineY = first.GetBaseline().GetStartPoint().Get(1);
//                 float textCodeX = 0f;
//                 float textCodeY = -(pageHeight - baselineY);

//                 return new TextObjectRecord
//                 {
//                     Id = recordId,
//                     BoundaryX = minX,
//                     BoundaryY = pageHeight - maxY,
//                     BoundaryW = Math.Max(0.1f, maxX - minX),
//                     BoundaryH = Math.Max(0.1f, maxY - minY),
//                     FontId = fontId,
//                     FontSize = fontSize,
//                     CtmA = c[0],
//                     CtmB = c[1],
//                     CtmC = c[2],
//                     CtmD = c[3],
//                     CtmE = c[4],
//                     CtmF = c[5],
//                     TextCodeX = textCodeX,
//                     TextCodeY = textCodeY,
//                     DeltaX = deltaX == null ? string.Empty : string.Join(" ", deltaX.Select(d => d.ToString("F4", Culture))),
//                     Text = string.Concat(renderInfos.Select(r => r.GetText()))
//                 };
//             }
//         }

//         public List<TextObjectRecord> Extract(string pdfPath)
//         {
//             var outList = new List<TextObjectRecord>();
//             int recordId = 1;
//             using (var pdf = new PdfDocument(new PdfReader(pdfPath)))
//             {
//                 for (int p = 1; p <= pdf.GetNumberOfPages(); p++)
//                 {
//                     var page = pdf.GetPage(p);
//                     float pageHeight = page.GetPageSize().GetHeight();

//                     var strategy = new SimpleStrategy();
//                     PdfTextExtractor.GetTextFromPage(page, strategy);
//                     strategy.Flush();

//                     foreach (var group in strategy.Groups)
//                     {
//                         var mapper = new OFDMapper();
//                         var rec = mapper.Map(group.RenderInfos, pageHeight, recordId++, group.DeltaXs);
//                         outList.Add(rec);
//                     }
//                 }
//             }

//             return outList;
//         }

//         private class SimpleStrategy : ITextExtractionStrategy
//         {
//             private readonly List<Group> groups = new List<Group>();
//             private readonly List<TextRenderInfo> current = new List<TextRenderInfo>();
//             private readonly List<float> currentDeltas = new List<float>();

//             public IReadOnlyList<Group> Groups => groups;

//             public void EventOccurred(IEventData data, EventType type)
//             {
//                 if (type != EventType.RENDER_TEXT) return;
//                 var tr = (TextRenderInfo)data;
//                 var txt = tr.GetText();
//                 if (string.IsNullOrEmpty(txt)) return;

//                 if (current.Count > 0)
//                 {
//                     var last = current.Last();
//                     float lastX = last.GetBaseline().GetStartPoint().Get(0);
//                     float curX = tr.GetBaseline().GetStartPoint().Get(0);
//                     float gap = curX - lastX;
//                     if (Math.Abs(tr.GetBaseline().GetStartPoint().Get(1) - last.GetBaseline().GetStartPoint().Get(1)) > 8f || gap < -10f || gap > last.GetAscentLine().GetBoundingRectangle().GetWidth() * 4)
//                     {
//                         // break
//                         FlushCurrent();
//                     }
//                     else
//                     {
//                         currentDeltas.Add(gap);
//                     }
//                 }

//                 current.Add(tr);
//             }

//             public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };

//             public string GetResultantText() => string.Empty;

//             public void FlushPendingWord() => FlushCurrent();
//             public void Flush() => FlushCurrent();

//             private void FlushCurrent()
//             {
//                 if (current.Count == 0) return;
//                 var g = new Group { RenderInfos = current.ToList(), DeltaXs = currentDeltas.Count==0?null:currentDeltas.ToList() };
//                 groups.Add(g);
//                 current.Clear();
//                 currentDeltas.Clear();
//             }

//             internal class Group
//             {
//                 public List<TextRenderInfo> RenderInfos { get; set; } = new List<TextRenderInfo>();
//                 public List<float>? DeltaXs { get; set; }
//             }
//         }

//         public class TextObjectRecord
//         {
//             private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

//             public int Id { get; set; }
//             public float BoundaryX { get; set; }
//             public float BoundaryY { get; set; }
//             public float BoundaryW { get; set; }
//             public float BoundaryH { get; set; }
//             public int FontId { get; set; }
//             public float FontSize { get; set; }
//             public float CtmA { get; set; }
//             public float CtmB { get; set; }
//             public float CtmC { get; set; }
//             public float CtmD { get; set; }
//             public float CtmE { get; set; }
//             public float CtmF { get; set; }
//             public float TextCodeX { get; set; }
//             public float TextCodeY { get; set; }
//             public string DeltaX { get; set; } = string.Empty;
//             public string Text { get; set; } = string.Empty;

//             public static string Header => "ID,Boundary_X,Boundary_Y,Boundary_W,Boundary_H,Font,Size,CTM_A,CTM_B,CTM_C,CTM_D,CTM_E,CTM_F,TextCode_X,TextCode_Y,DeltaX,Text";

//             public string ToCsvLine()
//             {
//                 var values = new List<string>
//                 {
//                     Id.ToString(Culture),
//                     BoundaryX.ToString("F4", Culture),
//                     BoundaryY.ToString("F4", Culture),
//                     BoundaryW.ToString("F4", Culture),
//                     BoundaryH.ToString("F4", Culture),
//                     FontId.ToString(Culture),
//                     FontSize.ToString("F2", Culture),
//                     CtmA.ToString("F6", Culture),
//                     CtmB.ToString("F6", Culture),
//                     CtmC.ToString("F6", Culture),
//                     CtmD.ToString("F6", Culture),
//                     CtmE.ToString("F6", Culture),
//                     CtmF.ToString("F6", Culture),
//                     TextCodeX.ToString("F4", Culture),
//                     TextCodeY.ToString("F4", Culture),
//                     string.IsNullOrEmpty(DeltaX) ? string.Empty : $"\"{DeltaX}\""
//                 };

//                 string esc = Text.Replace("\"", "\"\"");
//                 values.Add($"\"{esc}\"");
//                 return string.Join(',', values);
//             }
//         }

//         // small helper reference equality comparer for PdfFont keys
//         private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
//         {
//             public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();
//             public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
//             public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
//         }
//     }
// }
