using System;
using System.IO;
using OfdrwNet.Layout.Element.Canvas;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 单元对象
    /// <para>
    /// 绘制行为详见渲染器：CellContentDrawer
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2023-11-21 19:22:28
    /// </summary>
    public class Cell : Canvas
    {
        /// <summary>
        /// 单元格内容绘制器
        /// </summary>
        private readonly CellContentDrawer cellDrawer;

        /// <summary>
        /// 创建单元对象
        /// </summary>
        /// <param name="width">宽度（单位：毫米mm）</param>
        /// <param name="height">高度（单位：毫米mm）</param>
        public Cell(double width, double height) : base(width, height)
        {
            cellDrawer = new CellContentDrawer(this);
            SetDrawer(cellDrawer);
        }

        /// <summary>
        /// 创建单元对象
        /// </summary>
        /// <param name="x">x坐标（单位：毫米mm）</param>
        /// <param name="y">y坐标（单位：毫米mm）</param>
        /// <param name="w">宽度（单位：毫米mm）</param>
        /// <param name="h">高度（单位：毫米mm）</param>
        public Cell(double x, double y, double w, double h) : base(x, y, w, h)
        {
            cellDrawer = new CellContentDrawer(this);
            SetDrawer(cellDrawer);
        }

        /// <summary>
        /// 设置单元格内容绘制器
        /// </summary>
        /// <param name="drawer">新的绘制器</param>
        /// <returns>this</returns>
        public override Canvas SetDrawer(IDrawer drawer)
        {
            if (!(drawer is CellContentDrawer))
            {
                throw new ArgumentException("Cell的绘制器必须是CellContentDrawer");
            }
            base.SetDrawer(drawer);
            return this;
        }

        /// <summary>
        /// 获取单元格内容绘制器
        /// </summary>
        /// <returns>单元格内容绘制器</returns>
        public new CellContentDrawer GetDrawer()
        {
            return cellDrawer;
        }

        /// <summary>
        /// 获取单元格文字内容
        /// </summary>
        /// <returns>单元格文字内容</returns>
        public string? GetValue()
        {
            return cellDrawer.GetValue();
        }

        /// <summary>
        /// 设置单元格文字内容
        /// </summary>
        /// <param name="value">单元格文字内容</param>
        /// <returns>this</returns>
        public Cell SetValue(string value)
        {
            cellDrawer.SetValue(value);
            return this;
        }

        /// <summary>
        /// 设置图片
        /// </summary>
        /// <param name="imgPath">图片路径，仅支持png、jpg、jpeg、gif、bmp格式</param>
        /// <param name="w">图片宽度，单位：毫米</param>
        /// <param name="h">图片高度，单位：毫米</param>
        /// <returns>this</returns>
        public Cell SetValue(string imgPath, double w, double h)
        {
            cellDrawer.SetValue(imgPath, w, h);
            return this;
        }

        /// <summary>
        /// 设置图片
        /// <para>
        /// 图片宽度与高度通过图片文件自动获取并转换为毫米
        /// </para>
        /// </summary>
        /// <param name="imgPath">图片路径，仅支持png、jpg、jpeg、gif、bmp格式</param>
        /// <returns>this</returns>
        /// <exception cref="IOException">图片加载异常</exception>
        public Cell SetValue(string imgPath)
        {
            cellDrawer.SetValue(imgPath);
            return this;
        }

        /// <summary>
        /// 获取单元格颜色
        /// </summary>
        /// <returns>单元格颜色，格式：#000000、rgb(0,0,0)、rgba(0,0,0,1)</returns>
        public string? GetColor()
        {
            return cellDrawer.GetColor();
        }

        /// <summary>
        /// 设置单元格颜色
        /// </summary>
        /// <param name="color">单元格颜色，格式：#000000、rgb(0,0,0)、rgba(0,0,0,1)</param>
        /// <returns>this</returns>
        public Cell SetColor(string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                throw new ArgumentException("颜色(color)不能为空");
            }
            cellDrawer.SetColor(color);
            return this;
        }

        /// <summary>
        /// 获取字体名称
        /// </summary>
        /// <returns>字体名称</returns>
        public string? GetFontName()
        {
            return cellDrawer.GetFontName();
        }

        /// <summary>
        /// 设置字体名称
        /// </summary>
        /// <param name="fontName">字体名称，仅支持系统安装字体，且不会嵌入到OFD中。</param>
        /// <returns>this</returns>
        public Cell SetFontName(string fontName)
        {
            cellDrawer.SetFontName(fontName);
            return this;
        }

        /// <summary>
        /// 设置单元格使用的外部字体
        /// <para>
        /// 注意OFDRW不会提供任何字体裁剪功能，您的字体文件将直接加入OFD文件中，这可能造成文件体积剧增。
        /// </para>
        /// </summary>
        /// <param name="fontName">字体名称，如"思源宋体"</param>
        /// <param name="fontPath">字体文件所在路径</param>
        /// <returns>this</returns>
        public Cell SetFont(string fontName, string fontPath)
        {
            cellDrawer.SetFont(fontName, fontPath);
            return this;
        }

        /// <summary>
        /// 获取字号
        /// </summary>
        /// <returns>字号，默认：0.353 （单位：毫米）</returns>
        public double GetFontSize()
        {
            return cellDrawer.GetFontSize();
        }

        /// <summary>
        /// 设置字号
        /// </summary>
        /// <param name="fontSize">字号，默认：3（单位：毫米）</param>
        /// <returns>this</returns>
        public Cell SetFontSize(double fontSize)
        {
            cellDrawer.SetFontSize(fontSize);
            return this;
        }

        /// <summary>
        /// 获取文字对齐方式
        /// </summary>
        /// <returns>文字对齐方式</returns>
        public TextAlign GetTextAlign()
        {
            return cellDrawer.GetTextAlign();
        }

        /// <summary>
        /// 设置文字对齐方式
        /// </summary>
        /// <param name="textAlign">文字对齐方式</param>
        /// <returns>this</returns>
        public Cell SetTextAlign(TextAlign textAlign)
        {
            cellDrawer.SetTextAlign(textAlign);
            return this;
        }
    }
}