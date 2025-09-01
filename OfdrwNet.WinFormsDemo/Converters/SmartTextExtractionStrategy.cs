//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using iTextSharp.text.pdf.parser;

//namespace OfdrwNet.WinFormsDemo.Converters
//{
//    /// <summary>
//    /// �Ľ����ı���ȡ���ԣ�
//    /// 1. �� Y ����ۺ�ͬһ���ַ�
//    /// 2. �� X �������򲢸����ַ��������Ƿ����ո�
//    /// 3. ���˴������ַ�������ɵġ�α���š�����
//    /// </summary>
//    internal class SmartTextExtractionStrategy : ITextExtractionStrategy
//    {
//        private class CharInfo
//        {
//            public float X;
//            public float EndX;
//            public string Text = string.Empty;
//        }

//        // �м��ϣ�key = ��һ����� Y ����
//        private readonly Dictionary<float, List<CharInfo>> _lines = new();
//        private readonly float _yTolerance; // Y �ϲ��ݲ�
//        private readonly float _spaceThresholdMultiplier; // �ո���ֵ����

//        public SmartTextExtractionStrategy(float yTolerance = 2.0f, float spaceThresholdMultiplier = 0.5f)
//        {
//            _yTolerance = yTolerance;
//            _spaceThresholdMultiplier = spaceThresholdMultiplier;
//        }

//        public void RenderText(TextRenderInfo renderInfo)
//        {
//            // �ж��Ƿ�Ϊ��ת 90/270 �ȣ����ţ���������ֱ�����ַ����б���
//            var baseline = renderInfo.GetBaseline();
//            var start = baseline.GetStartPoint();
//            var end = baseline.GetEndPoint();
//            float dx = Math.Abs(end[Vector.I1] - start[Vector.I1]);
//            float dy = Math.Abs(end[Vector.I2] - start[Vector.I2]);
//            bool vertical = dy > dx * 3; // ������ŵĴ����ж�

//            var text = renderInfo.GetText();
//            if (string.IsNullOrEmpty(text)) return;

//            // ����ƽ���ַ�����
//            var ascent = renderInfo.GetAscentLine().GetEndPoint();
//            float charWidth = dx / Math.Max(1, text.Length);
//            if (charWidth <= 0) charWidth = renderInfo.GetSingleSpaceWidth();
//            if (charWidth <= 0) charWidth = 5f;

//            float y = start[Vector.I2];
//            float normalizedY = NormalizeY(y);

//            if (!_lines.TryGetValue(normalizedY, out var list))
//            {
//                list = new List<CharInfo>();
//                _lines[normalizedY] = list;
//            }

//            if (vertical)
//            {
//                // ���ţ�ÿ���ַ�������Ϊһ�У�������ϲ�
//                foreach (var ch in text)
//                {
//                    var yv = NormalizeY(y);
//                    _lines[yv] = new List<CharInfo>
//                    {
//                        new CharInfo{ X = start[Vector.I1], EndX = start[Vector.I1] + charWidth, Text = ch.ToString() }
//                    };
//                    y += charWidth; // ģ������ƫ��
//                }
//            }
//            else
//            {
//                list.Add(new CharInfo
//                {
//                    X = start[Vector.I1],
//                    EndX = end[Vector.I1],
//                    Text = text
//                });
//            }
//        }

//        public void BeginTextBlock() { }
//        public void EndTextBlock() { }
//        public void RenderImage(ImageRenderInfo renderInfo) { }

//        public string GetResultantText()
//        {
//            if (_lines.Count == 0) return string.Empty;
//            var sb = new StringBuilder();
//            // PDF ����ϵԭ�������£����԰� Y �Ӹߵ����������
//            foreach (var kv in _lines.OrderByDescending(k => k.Key))
//            {
//                var chars = kv.Value.OrderBy(c => c.X).ToList();
//                if (chars.Count == 0) continue;

//                // �������ƽ���ַ����ȣ����ڿո��жϣ�
//                float avgWidth = chars.Average(c => Math.Max(1f, c.EndX - c.X));
//                float spaceThreshold = avgWidth * (1 + _spaceThresholdMultiplier);

//                float lastEnd = chars[0].X;
//                var lineBuilder = new StringBuilder();
//                foreach (var ci in chars)
//                {
//                    float gap = ci.X - lastEnd;
//                    if (gap > spaceThreshold)
//                    {
//                        lineBuilder.Append(' ');
//                    }
//                    lineBuilder.Append(ci.Text);
//                    lastEnd = ci.EndX;
//                }
//                var line = lineBuilder.ToString().TrimEnd();
//                sb.AppendLine(line);
//            }

//            return PostProcess(sb.ToString());
//        }

//        private float NormalizeY(float y)
//        {
//            // �� y ��һ�����ݲ��Ͱ�����ٸ������
//            return (float)Math.Round(y / _yTolerance, MidpointRounding.AwayFromZero) * _yTolerance;
//        }

//        private string PostProcess(string text)
//        {
//            if (string.IsNullOrWhiteSpace(text)) return text;
//            var lines = text.Replace("\r", "").Split('\n');
//            // ������ 70% �����ǵ��ַ��������ٴκϲ�
//            int single = lines.Count(l => l.Trim().Length == 1);
//            if (lines.Length > 5 && single > lines.Length * 0.7)
//            {
//                var sb = new StringBuilder();
//                foreach (var l in lines)
//                {
//                    var t = l.Trim();
//                    if (t.Length == 1 && !string.IsNullOrWhiteSpace(t))
//                    {
//                        sb.Append(t);
//                    }
//                    else if (t.Length > 1)
//                    {
//                        if (sb.Length > 0)
//                        {
//                            sb.AppendLine();
//                        }
//                        sb.AppendLine(t);
//                    }
//                }
//                return sb.ToString();
//            }
//            return text;
//        }
//    }
//}
//#if false
//// 旧 iTextSharp SmartTextExtractionStrategy 已停用，迁移完成后删除。
//using System; using System.Collections.Generic; using System.Linq; using System.Text; using iTextSharp.text.pdf.parser;
//namespace OfdrwNet.WinFormsDemo.Converters { internal class SmartTextExtractionStrategy : ITextExtractionStrategy { public void RenderText(TextRenderInfo renderInfo){} public void BeginTextBlock(){} public void EndTextBlock(){} public void RenderImage(ImageRenderInfo renderInfo){} public string GetResultantText()=>string.Empty; } }
//#endif
