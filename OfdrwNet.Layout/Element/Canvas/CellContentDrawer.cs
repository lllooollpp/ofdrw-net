using System;
using System.IO;
using OfdrwNet.Layout.Element.Canvas;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 单元格内容绘制器
    /// <para>
    /// 负责绘制单元格中的文本和图像内容
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2023-11-21 19:22:28
    /// </summary>
    public class CellContentDrawer : IDrawer
    {
        private readonly Cell cell;
        private string? value;
        private string? imagePath;
        private double imageWidth;
        private double imageHeight;
        private string color = "#000000";
        private string fontName = "SimSun";
        private string? fontPath;
        private double fontSize = 3.0;
        private TextAlign textAlign = TextAlign.Start;

        /// <summary>
        /// 创建单元格内容绘制器
        /// </summary>
        /// <param name="cell">关联的单元格</param>
        public CellContentDrawer(Cell cell)
        {
            this.cell = cell ?? throw new ArgumentNullException(nameof(cell));
        }

        /// <summary>
        /// 绘制单元格内容
        /// </summary>
        /// <param name="ctx">绘制上下文</param>
        public void Draw(DrawContext ctx)
        {
            // 实现单元格内容的绘制逻辑
            if (!string.IsNullOrEmpty(value))
            {
                DrawText(ctx);
            }
            else if (!string.IsNullOrEmpty(imagePath))
            {
                DrawImage(ctx);
            }
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        /// <param name="ctx">绘制上下文</param>
        private void DrawText(DrawContext ctx)
        {
            // 设置填充样式
            ctx.FillStyle = color;
            
            // 设置字体
            ctx.Font = $"{fontSize}mm {fontName}";
            
            // 绘制文本
            ctx.FillText(value!, 0, 0);
        }

        /// <summary>
        /// 绘制图像
        /// </summary>
        /// <param name="ctx">绘制上下文</param>
        private void DrawImage(DrawContext ctx)
        {
            // 绘制图像
            ctx.DrawImage(imagePath!, 0, 0, imageWidth, imageHeight);
        }

        /// <summary>
        /// 获取单元格文字内容
        /// </summary>
        /// <returns>文字内容</returns>
        public string? GetValue()
        {
            return value;
        }

        /// <summary>
        /// 设置单元格文字内容
        /// </summary>
        /// <param name="value">文字内容</param>
        public void SetValue(string value)
        {
            this.value = value;
            imagePath = null; // 清除图片
        }

        /// <summary>
        /// 设置图片
        /// </summary>
        /// <param name="imgPath">图片路径</param>
        /// <param name="w">图片宽度</param>
        /// <param name="h">图片高度</param>
        public void SetValue(string imgPath, double w, double h)
        {
            imagePath = imgPath;
            imageWidth = w;
            imageHeight = h;
            value = null; // 清除文字
        }

        /// <summary>
        /// 设置图片（自动获取尺寸）
        /// </summary>
        /// <param name="imgPath">图片路径</param>
        public void SetValue(string imgPath)
        {
            // 简化实现，使用默认尺寸
            SetValue(imgPath, 10, 10);
        }

        /// <summary>
        /// 获取颜色
        /// </summary>
        /// <returns>颜色</returns>
        public string GetColor()
        {
            return color;
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetColor(string color)
        {
            this.color = color;
        }

        /// <summary>
        /// 获取字体名称
        /// </summary>
        /// <returns>字体名称</returns>
        public string GetFontName()
        {
            return fontName;
        }

        /// <summary>
        /// 设置字体名称
        /// </summary>
        /// <param name="fontName">字体名称</param>
        public void SetFontName(string fontName)
        {
            this.fontName = fontName;
        }

        /// <summary>
        /// 设置字体
        /// </summary>
        /// <param name="fontName">字体名称</param>
        /// <param name="fontPath">字体路径</param>
        public void SetFont(string fontName, string fontPath)
        {
            this.fontName = fontName;
            this.fontPath = fontPath;
        }

        /// <summary>
        /// 获取字号
        /// </summary>
        /// <returns>字号</returns>
        public double GetFontSize()
        {
            return fontSize;
        }

        /// <summary>
        /// 设置字号
        /// </summary>
        /// <param name="fontSize">字号</param>
        public void SetFontSize(double fontSize)
        {
            this.fontSize = fontSize;
        }

        /// <summary>
        /// 获取文字对齐方式
        /// </summary>
        /// <returns>文字对齐方式</returns>
        public TextAlign GetTextAlign()
        {
            return textAlign;
        }

        /// <summary>
        /// 设置文字对齐方式
        /// </summary>
        /// <param name="textAlign">文字对齐方式</param>
        public void SetTextAlign(TextAlign textAlign)
        {
            this.textAlign = textAlign;
        }
    }
}