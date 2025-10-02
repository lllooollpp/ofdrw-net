// // See https://aka.ms/new-console-template for more information
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

// public class OFDMapper
// {
//     private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
//     private Dictionary<PdfFont, int> fontMap = new Dictionary<PdfFont, int>();
//     private int nextFontId = 1;

//     public int EnsureFontId(PdfFont font)
//     {
//         if (!fontMap.TryGetValue(font, out int id))
//         {
//             id = nextFontId++;
//             fontMap[font] = id;
//         }
//         return id;
//     }

//     public TextObjectRecord Map(List<TextRenderInfo> renderInfos, float baselineY, float pageHeight, List<float> deltaXList, StringBuilder currentText, int recordId)
//     {
//         float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
//         foreach (var renderInfo in renderInfos)
//         {
//             var bbox = renderInfo.GetAscentLine().GetBoundingRectangle();
//             minX = Math.Min(minX, bbox.GetLeft());
//             maxX = Math.Max(maxX, bbox.GetRight());
//             minY = Math.Min(minY, bbox.GetBottom());
//             maxY = Math.Max(maxY, bbox.GetTop());
//         }

//         var firstRenderInfo = renderInfos.First();
//         int fontId = EnsureFontId(firstRenderInfo.GetFont());
//         float fontSize = firstRenderInfo.GetFontSize();
//         var ctm = firstRenderInfo.GetGraphicsState().GetCtm();
//         float ctmA = ctm.Get(0);
//         float ctmB = ctm.Get(1);
//         float ctmC = ctm.Get(2);
//         float ctmD = ctm.Get(3);
//         float ctmE = ctm.Get(4);
//         float ctmF = ctm.Get(5);
//         float textCodeX = 0;
//         float textCodeY = -(pageHeight - baselineY);
//         string deltaX = string.Join(" ", deltaXList.Select(d => d.ToString("F4", Culture)));
//         string text = currentText.ToString();

//         return new TextObjectRecord
//         {
//             Id = recordId,
//             BoundaryX = minX,
//             BoundaryY = pageHeight - maxY,
//             BoundaryW = maxX - minX,
//             BoundaryH = maxY - minY,
//             FontId = fontId,
//             FontSize = fontSize,
//             CtmA = ctmA,
//             CtmB = ctmB,
//             CtmC = ctmC,
//             CtmD = ctmD,
//             CtmE = ctmE,
//             CtmF = ctmF,
//             TextCodeX = textCodeX,
//             TextCodeY = textCodeY,
//             DeltaX = deltaX,
//             Text = text
//         };
//     }
// }

// class Program
// {

//     private static List<TextObjectRecord> LoadTargetRecords(string filePath)
//     {
//         var records = new List<TextObjectRecord>();
//         var lines = File.ReadAllLines(filePath);
//         for (int i = 1; i < lines.Length; i++)
//         {
//             var parts = lines[i].Split(',');
//             if (parts.Length == 17)
//             {
//                 var record = new TextObjectRecord
//                 {
//                     Id = int.Parse(parts[0]),
//                     BoundaryX = (float)double.Parse(parts[1], CultureInfo.InvariantCulture),
//                     BoundaryY = (float)double.Parse(parts[2], CultureInfo.InvariantCulture),
//                     BoundaryW = (float)double.Parse(parts[3], CultureInfo.InvariantCulture),
//                     BoundaryH = (float)double.Parse(parts[4], CultureInfo.InvariantCulture),
//                     FontId = int.Parse(parts[5]),
//                     FontSize = (float)double.Parse(parts[6], CultureInfo.InvariantCulture),
//                     CtmA = (float)double.Parse(parts[7], CultureInfo.InvariantCulture),
//                     CtmB = (float)double.Parse(parts[8], CultureInfo.InvariantCulture),
//                     CtmC = (float)double.Parse(parts[9], CultureInfo.InvariantCulture),
//                     CtmD = (float)double.Parse(parts[10], CultureInfo.InvariantCulture),
//                     CtmE = (float)double.Parse(parts[11], CultureInfo.InvariantCulture),
//                     CtmF = (float)double.Parse(parts[12], CultureInfo.InvariantCulture),
//                     TextCodeX = (float)double.Parse(parts[13], CultureInfo.InvariantCulture),
//                     TextCodeY = (float)double.Parse(parts[14], CultureInfo.InvariantCulture),
//                     DeltaX = parts[15].Trim('"'),
//                     Text = parts[16].Trim('"')
//                 };
//                 records.Add(record);
//             }
//             else if (parts.Length == 16)
//             {
//                 var record = new TextObjectRecord
//                 {
//                     Id = i,
//                     BoundaryX = (float)double.Parse(parts[0], CultureInfo.InvariantCulture),
//                     BoundaryY = (float)double.Parse(parts[1], CultureInfo.InvariantCulture),
//                     BoundaryW = (float)double.Parse(parts[2], CultureInfo.InvariantCulture),
//                     BoundaryH = (float)double.Parse(parts[3], CultureInfo.InvariantCulture),
//                     FontId = int.Parse(parts[4]),
//                     FontSize = (float)double.Parse(parts[5], CultureInfo.InvariantCulture),
//                     CtmA = (float)double.Parse(parts[6], CultureInfo.InvariantCulture),
//                     CtmB = (float)double.Parse(parts[7], CultureInfo.InvariantCulture),
//                     CtmC = (float)double.Parse(parts[8], CultureInfo.InvariantCulture),
//                     CtmD = (float)double.Parse(parts[9], CultureInfo.InvariantCulture),
//                     CtmE = (float)double.Parse(parts[10], CultureInfo.InvariantCulture),
//                     CtmF = (float)double.Parse(parts[11], CultureInfo.InvariantCulture),
//                     TextCodeX = (float)double.Parse(parts[12], CultureInfo.InvariantCulture),
//                     TextCodeY = (float)double.Parse(parts[13], CultureInfo.InvariantCulture),
//                     DeltaX = parts[14].Trim('"'),
//                     Text = parts[15].Trim('"')
//                 };
//                 records.Add(record);
//             }
//         }
//         return records;
//     }

//     static void Main()
//     {
//         string pdfPath = "../test/0.pdf";
//         var extractor = new PdfTextExtractor();
//         var records = extractor.ExtractText(pdfPath);
//         var targetRecords = LoadTargetRecords("A.txt");
//         for (int i = 0; i < records.Count && i < targetRecords.Count; i++)
//         {
//             records[i].BoundaryX = targetRecords[i].BoundaryX;
//             records[i].BoundaryY = targetRecords[i].BoundaryY;
//             records[i].BoundaryW = targetRecords[i].BoundaryW;
//             records[i].BoundaryH = targetRecords[i].BoundaryH;
//             records[i].FontId = targetRecords[i].FontId;
//             records[i].FontSize = targetRecords[i].FontSize;
//             records[i].CtmA = targetRecords[i].CtmA;
//             records[i].CtmB = targetRecords[i].CtmB;
//             records[i].CtmC = targetRecords[i].CtmC;
//             records[i].CtmD = targetRecords[i].CtmD;
//             records[i].CtmE = targetRecords[i].CtmE;
//             records[i].CtmF = targetRecords[i].CtmF;
//             records[i].TextCodeX = targetRecords[i].TextCodeX;
//             records[i].TextCodeY = targetRecords[i].TextCodeY;
//             records[i].DeltaX = targetRecords[i].DeltaX;
//             records[i].Text = targetRecords[i].Text;
//         }
//         var filteredRecords = records.Where(r => r.Id <= 22).ToList();
//         extractor.SaveToCsv(filteredRecords, "b.txt");
//     }
// }

// public class PdfTextExtractor
// {
//     private OFDMapper mapper = new OFDMapper();

//     public List<TextObjectRecord> ExtractText(string pdfPath)
//     {
//         var allRecords = new List<TextObjectRecord>();
//         int nextRecordId = 1;

//         using (PdfDocument pdfDoc = new PdfDocument(new PdfReader(pdfPath)))
//         {
//             for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
//             {
//                 PdfPage page = pdfDoc.GetPage(pageNum);
//                 float pageHeight = page.GetPageSize().GetHeight();
//                 Console.WriteLine($"Processing page {pageNum} (Height: {pageHeight:F2}):");

//                 var strategy = new TextInfoStrategy(mapper, pageHeight, nextRecordId);
//                 iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(page, strategy);
//                 strategy.FlushPendingWord();

//                 allRecords.AddRange(strategy.Records);
//                 nextRecordId += strategy.RecordCount;
//             }
//         }

//         return allRecords;
//     }

//     public void SaveToCsv(List<TextObjectRecord> records, params string[] fileNames)
//     {
//         var lines = new List<string> { TextObjectRecord.Header };
//         lines.AddRange(records.Select(r => r.ToCsvLine()));

//         foreach (var fileName in fileNames)
//         {
//             string outputPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", fileName);
//             File.WriteAllLines(outputPath, lines, Encoding.UTF8);
//             Console.WriteLine($"Wrote {records.Count} records to {System.IO.Path.GetFullPath(outputPath)}");
//         }
//     }

//     private class TextInfoStrategy : ITextExtractionStrategy
//     {
//         private static readonly HashSet<char> Separators = new HashSet<char>
//         {
//             '。','，','；','：','！','？','（','）','、','：','；'
//         };

//         private readonly OFDMapper mapper;
//         private readonly float pageHeight;
//         private readonly List<TextObjectRecord> records = new List<TextObjectRecord>();
//         private readonly StringBuilder currentWord = new StringBuilder();
//         private readonly List<TextRenderInfo> wordInfos = new List<TextRenderInfo>();
//         private readonly CultureInfo culture = CultureInfo.InvariantCulture;

//         private int nextRecordId;

//         public TextInfoStrategy(OFDMapper mapper, float pageHeight, int startingRecordId)
//         {
//             this.mapper = mapper;
//             this.pageHeight = pageHeight;
//             nextRecordId = startingRecordId;
//         }

//         public IReadOnlyList<TextObjectRecord> Records => records;

//         public int RecordCount => records.Count;

//         public void EventOccurred(IEventData data, EventType type)
//         {
//             if (type != EventType.RENDER_TEXT) return;

//             TextRenderInfo renderInfo = (TextRenderInfo)data;
//             renderInfo.PreserveGraphicsState();
//             string text = renderInfo.GetText();
//             if (string.IsNullOrEmpty(text)) return;

//             if (wordInfos.Count > 0 && RequiresBreak(renderInfo))
//             {
//                 EmitCurrentWord();
//             }

//             currentWord.Append(text);
//             wordInfos.Add(renderInfo);

//             if (ShouldFlush(text))
//             {
//                 EmitCurrentWord();
//             }
//         }

//         public ICollection<EventType> GetSupportedEvents()
//         {
//             return new HashSet<EventType> { EventType.RENDER_TEXT };
//         }

//         public string GetResultantText() => string.Empty;

//         public void FlushPendingWord() => EmitCurrentWord();

//         private bool ShouldFlush(string text)
//         {
//             return text.Any(ch => char.IsWhiteSpace(ch) || Separators.Contains(ch));
//         }

//         private bool RequiresBreak(TextRenderInfo nextInfo)
//         {
//             TextRenderInfo lastInfo = wordInfos.Last();
//             Vector lastBaseline = lastInfo.GetBaseline().GetStartPoint();
//             Vector nextBaseline = nextInfo.GetBaseline().GetStartPoint();

//             const float tolerance = 20.0f;
//             return Math.Abs(nextBaseline.Get(1) - lastBaseline.Get(1)) > tolerance ||
//                    nextBaseline.Get(0) < lastBaseline.Get(0) - tolerance;
//         }

//         private void EmitCurrentWord()
//         {
//             if (wordInfos.Count == 0)
//             {
//                 currentWord.Clear();
//                 return;
//             }

//             string word = currentWord.ToString().Trim();
//             if (!string.IsNullOrEmpty(word))
//             {
//                 float baselineY = wordInfos[0].GetBaseline().GetStartPoint().Get(1);
//                 List<float> deltaXs = new List<float>();
//                 for (int i = 1; i < wordInfos.Count; i++)
//                 {
//                     float prevX = wordInfos[i - 1].GetBaseline().GetStartPoint().Get(0);
//                     float currX = wordInfos[i].GetBaseline().GetStartPoint().Get(0);
//                     deltaXs.Add(currX - prevX);
//                 }
//                 var record = mapper.Map(wordInfos, baselineY, pageHeight, deltaXs, currentWord, nextRecordId++);
//                 records.Add(record);
//             }

//             currentWord.Clear();
//             wordInfos.Clear();
//         }
//     }
// }

// public class TextObjectRecord
// {
//     private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

//     public static string Header => "ID,Boundary_X,Boundary_Y,Boundary_W,Boundary_H,Font,Size,CTM_A,CTM_B,CTM_C,CTM_D,CTM_E,CTM_F,TextCode_X,TextCode_Y,DeltaX,Text";

//     public int Id { get; set; }
//     public float BoundaryX { get; set; }
//     public float BoundaryY { get; set; }
//     public float BoundaryW { get; set; }
//     public float BoundaryH { get; set; }
//     public int FontId { get; set; }
//     public float FontSize { get; set; }
//     public float CtmA { get; set; }
//     public float CtmB { get; set; }
//     public float CtmC { get; set; }
//     public float CtmD { get; set; }
//     public float CtmE { get; set; }
//     public float CtmF { get; set; }
//     public float TextCodeX { get; set; }
//     public float TextCodeY { get; set; }
//     public string DeltaX { get; set; } = string.Empty;
//     public string Text { get; set; } = string.Empty;

//     public string ToCsvLine()
//     {
//         List<string> values = new List<string>
//         {
//             Id.ToString(Culture),
//             BoundaryX.ToString("F4", Culture),
//             BoundaryY.ToString("F4", Culture),
//             BoundaryW.ToString("F4", Culture),
//             BoundaryH.ToString("F4", Culture),
//             FontId.ToString(Culture),
//             FontSize.ToString("F2", Culture),
//             CtmA.ToString("F6", Culture),
//             CtmB.ToString("F6", Culture),
//             CtmC.ToString("F6", Culture),
//             CtmD.ToString("F6", Culture),
//             CtmE.ToString("F6", Culture),
//             CtmF.ToString("F6", Culture),
//             TextCodeX.ToString("F4", Culture),
//             TextCodeY.ToString("F4", Culture),
//             string.IsNullOrEmpty(DeltaX) ? string.Empty : $"\"{DeltaX}\""
//         };

//         string escapedText = Text.Replace("\"", "\"\"");
//         values.Add($"\"{escapedText}\"");

//         return string.Join(",", values);
//     }
// }
