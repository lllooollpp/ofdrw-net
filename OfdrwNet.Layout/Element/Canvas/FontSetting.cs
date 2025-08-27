using System;
using OfdrwNet.Font;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Element.Canvas
{
    /// <summary>
    /// 字体设置
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-05-06 18:21:02
    /// </summary>
    public class FontSetting : ICloneable
    {
        /// <summary>
        /// 字体对象
        /// </summary>
        private OfdrwNet.Font.Font fontObj;

        /// <summary>
        /// 规定字号 单位（毫米mm）
        /// </summary>
        private double fontSize;

        /// <summary>
        /// 斜体
        /// </summary>
        private bool italic = false;

        /// <summary>
        /// 规定字体的粗细。
        /// <para>
        /// 可能值为： 100、200、300、400、500、600、700、800、900
        /// </para>
        /// <para>
        /// 默认值： 400
        /// </para>
        /// <para>
        /// 预设加粗为：800
        /// </para>
        /// </summary>
        private int fontWeight = 400;

        /// <summary>
        /// 字间距
        /// </summary>
        private double letterSpacing = 0d;

        /// <summary>
        /// 字符方向
        /// <para>
        /// 指定了文字放置的方式（基线方向）
        /// </para>
        /// <para>
        /// 允许值：0、90、180、270
        /// </para>
        /// </summary>
        private int charDirection = 0;

        /// <summary>
        /// 阅读方向
        /// <para>
        /// 指定了文字排列的方向
        /// </para>
        /// <para>
        /// 允许值：0、90、180、270
        /// </para>
        /// </summary>
        private int readDirection = 0;

        /// <summary>
        /// 文本内容的当前对齐方式。
        /// </summary>
        private TextAlign textAlign = TextAlign.Start;

        /// <summary>
        /// 简化构造提供默认的字体配置
        /// <para>
        /// 字体类型为宋体
        /// </para>
        /// </summary>
        /// <returns>字体配置</returns>
        public static FontSetting GetInstance()
        {
            return new FontSetting(5, FontName.SimSun.ToFont());
        }

        /// <summary>
        /// 简化构造提供可选的字体配置
        /// <para>
        /// 字体类型为宋体
        /// </para>
        /// </summary>
        /// <param name="fontSize">字体大小，单位：毫米（mm）</param>
        /// <returns>字体配置</returns>
        public static FontSetting GetInstance(double fontSize)
        {
            return new FontSetting(fontSize, FontName.SimSun.ToFont());
        }

        /// <summary>
        /// 创建字体设置
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        /// <param name="fontObj">字体对象</param>
        public FontSetting(double fontSize, OfdrwNet.Font.Font fontObj)
        {
            this.fontObj = fontObj ?? throw new ArgumentNullException(nameof(fontObj), "字体对象(fontObj)为空");
            this.fontSize = fontSize;
        }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private FontSetting()
        {
        }

        /// <summary>
        /// 获取文本对齐方式
        /// </summary>
        /// <returns>文本对齐方式</returns>
        public TextAlign GetTextAlign()
        {
            return textAlign;
        }

        /// <summary>
        /// 设置文本对齐方式
        /// </summary>
        /// <param name="textAlign">文本对齐方式</param>
        /// <returns>this</returns>
        public FontSetting SetTextAlign(TextAlign textAlign)
        {
            this.textAlign = textAlign;
            return this;
        }

        /// <summary>
        /// 获取文字对象
        /// </summary>
        /// <returns>文字对象</returns>
        public OfdrwNet.Font.Font GetFont()
        {
            return fontObj;
        }

        /// <summary>
        /// 设置文字对象
        /// </summary>
        /// <param name="fontObj">文字对象</param>
        /// <returns>this</returns>
        public FontSetting SetFont(OfdrwNet.Font.Font fontObj)
        {
            this.fontObj = fontObj ?? throw new ArgumentNullException(nameof(fontObj), "字体对象(fontObj)为空");
            return this;
        }

        /// <summary>
        /// 获取文字字号
        /// </summary>
        /// <returns>字号（单位毫米）</returns>
        public double GetFontSize()
        {
            return fontSize;
        }

        /// <summary>
        /// 设置文字字号
        /// </summary>
        /// <param name="fontSize">字号（单位毫米）</param>
        /// <returns>this</returns>
        public FontSetting SetFontSize(double fontSize)
        {
            this.fontSize = fontSize;
            return this;
        }

        /// <summary>
        /// 字体是否为斜体
        /// </summary>
        /// <returns>true - 斜体</returns>
        public bool IsItalic()
        {
            return italic;
        }

        /// <summary>
        /// 设置字体是否为斜体
        /// </summary>
        /// <param name="italic">true - 斜体；false - 非斜体</param>
        /// <returns>this</returns>
        public FontSetting SetItalic(bool italic)
        {
            this.italic = italic;
            return this;
        }

        /// <summary>
        /// 获取字体粗细
        /// </summary>
        /// <returns>字体粗细</returns>
        public int GetFontWeight()
        {
            return fontWeight;
        }

        /// <summary>
        /// 设置字体粗细
        /// </summary>
        /// <param name="fontWeight">字体粗细</param>
        /// <returns>this</returns>
        public FontSetting SetFontWeight(int fontWeight)
        {
            this.fontWeight = fontWeight;
            return this;
        }

        /// <summary>
        /// 获取字间距
        /// </summary>
        /// <returns>字间距</returns>
        public double GetLetterSpacing()
        {
            return letterSpacing;
        }

        /// <summary>
        /// 设置字间距
        /// </summary>
        /// <param name="letterSpacing">字间距</param>
        /// <returns>this</returns>
        public FontSetting SetLetterSpacing(double letterSpacing)
        {
            this.letterSpacing = letterSpacing;
            return this;
        }

        /// <summary>
        /// 获取字符方向
        /// </summary>
        /// <returns>字符方向</returns>
        public int GetCharDirection()
        {
            return charDirection;
        }

        /// <summary>
        /// 设置字符方向
        /// </summary>
        /// <param name="charDirection">字符方向</param>
        /// <returns>this</returns>
        public FontSetting SetCharDirection(int charDirection)
        {
            this.charDirection = charDirection;
            return this;
        }

        /// <summary>
        /// 获取阅读方向
        /// </summary>
        /// <returns>阅读方向</returns>
        public int GetReadDirection()
        {
            return readDirection;
        }

        /// <summary>
        /// 设置阅读方向
        /// </summary>
        /// <param name="readDirection">阅读方向</param>
        /// <returns>this</returns>
        public FontSetting SetReadDirection(int readDirection)
        {
            this.readDirection = readDirection;
            return this;
        }

        /// <summary>
        /// 克隆字体设置
        /// </summary>
        /// <returns>克隆的字体设置</returns>
        public object Clone()
        {
            return new FontSetting
            {
                fontObj = fontObj,
                fontSize = fontSize,
                italic = italic,
                fontWeight = fontWeight,
                letterSpacing = letterSpacing,
                charDirection = charDirection,
                readDirection = readDirection,
                textAlign = textAlign
            };
        }
    }
}