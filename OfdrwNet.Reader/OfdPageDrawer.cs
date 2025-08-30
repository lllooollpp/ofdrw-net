using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// OFD 页面绘制器
    /// </summary>
    public class OfdPageDrawer : IDisposable
    {
        private readonly OfdReader _reader;
        private readonly Dictionary<long, System.Drawing.Font> _fontCache = new Dictionary<long, System.Drawing.Font>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reader">OFD阅读器</param>
        public OfdPageDrawer(OfdReader reader)
        {
            _reader = reader;
        }

        /// <summary>
        /// 绘制单个页面到图片
        /// </summary>
        /// <param name="pageNum">页码，从 1 开始</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片高度</param>
        /// <returns>绘制的页面图片</returns>
        public Bitmap DrawPageToBitmap(int pageNum, int width = 800, int height = 600)
        {
            var bitmap = new Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 设置高质量渲染
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // 清空背景
                g.Clear(Color.White);

                // 绘制页面内容
                DrawPage(g, pageNum);
            }
            return bitmap;
        }

        /// <summary>
        /// 绘制页面内容
        /// </summary>
        /// <param name="g">图形对象</param>
        /// <param name="pageNum">页码</param>
        private void DrawPage(System.Drawing.Graphics g, int pageNum)
        {
            try
            {
                var pageList = _reader.GetPageList();
                if (pageList == null || pageNum < 1 || pageNum > pageList.Count)
                    return;

                var pageInfo = pageList[pageNum - 1];
                if (pageInfo?.Obj == null)
                    return;

                // 解析页面内容
                var contentElements = pageInfo.Obj.Elements("Content");
                foreach (var contentElement in contentElements)
                {
                    var layerElements = contentElement.Elements("Layer");
                    foreach (var layerElement in layerElements)
                    {
                        DrawLayer(g, layerElement);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果出错，在页面上显示错误信息
                g.DrawString($"绘制页面{pageNum}时出错: {ex.Message}", 
                    new System.Drawing.Font("Arial", 12), Brushes.Red, 10, 10);
            }
        }

        /// <summary>
        /// 绘制图层内容
        /// </summary>
        private void DrawLayer(System.Drawing.Graphics g, XElement layerElement)
        {
            // 查找所有的TextObject元素
            var textObjects = layerElement.Descendants("TextObject");
            
            foreach (var textObject in textObjects)
            {
                DrawTextObject(g, textObject);
            }

            // 可以在这里添加其他对象类型的绘制，如PathObject, ImageObject等
        }

        /// <summary>
        /// 绘制文本对象
        /// </summary>
        private void DrawTextObject(System.Drawing.Graphics g, XElement textObjectElement)
        {
            try
            {
                // 获取文本内容
                var textCodes = textObjectElement.Descendants("TextCode");
                if (!textCodes.Any()) return;

                // 获取字体大小，默认为12
                var sizeAttr = textObjectElement.Attribute("Size");
                float fontSize = sizeAttr != null ? float.Parse(sizeAttr.Value) : 12f;

                // 获取边界框
                var boundaryAttr = textObjectElement.Attribute("Boundary");
                float x = 10, y = 30; // 默认位置
                if (boundaryAttr != null)
                {
                    var boundary = boundaryAttr.Value.Split(' ');
                    if (boundary.Length >= 2)
                    {
                        float.TryParse(boundary[0], out x);
                        float.TryParse(boundary[1], out y);
                    }
                }

                // 创建字体
                var font = new System.Drawing.Font("SimSun", fontSize, FontStyle.Regular);

                // 绘制每个TextCode
                float currentY = y;
                foreach (var textCode in textCodes)
                {
                    var text = textCode.Value?.Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        g.DrawString(text, font, Brushes.Black, x, currentY);
                        currentY += fontSize + 2; // 行间距
                    }
                }

                font.Dispose();
            }
            catch (Exception ex)
            {
                // 如果文本对象绘制失败，显示错误
                g.DrawString($"文本绘制错误: {ex.Message}", 
                    new System.Drawing.Font("Arial", 10), Brushes.Red, 10, 100);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            foreach (var font in _fontCache.Values)
            {
                font.Dispose();
            }
            _fontCache.Clear();
        }
    }
}
