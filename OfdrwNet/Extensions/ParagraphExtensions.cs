using OfdrwNet.Layout.Element;

namespace OfdrwNet.Extensions
{
    /// <summary>
    /// Paragraph扩展方法，提供向后兼容性
    /// </summary>
    public static class ParagraphExtensions
    {
        /// <summary>
        /// 获取段落的文本内容（兼容性方法）
        /// </summary>
        public static string? GetText(this Paragraph paragraph)
        {
            if (paragraph.Contents.Count == 0) return null;
            
            var text = new System.Text.StringBuilder();
            foreach (var span in paragraph.Contents)
            {
                // 获取Span的实际文本内容
                text.Append(span.Text ?? string.Empty);
            }
            return text.Length > 0 ? text.ToString() : null;
        }

        /// <summary>
        /// 获取段落的字体名称（兼容性方法）
        /// </summary>
        public static string? GetFontName(this Paragraph paragraph)
        {
            // 返回默认字体或第一个span的字体
            return "SimSun"; // 默认字体
        }

        /// <summary>
        /// 获取段落的字体大小（兼容性方法）
        /// </summary>
        public static double GetFontSize(this Paragraph paragraph)
        {
            return paragraph.DefaultFontSize ?? 12.0;
        }

        /// <summary>
        /// 获取段落的行高（兼容性方法）
        /// </summary>
        public static double GetLineHeight(this Paragraph paragraph)
        {
            return paragraph.LineSpace;
        }

        /// <summary>
        /// 设置默认字体大小
        /// </summary>
        public static Paragraph SetDefaultFontSize(this Paragraph paragraph, double fontSize)
        {
            paragraph.DefaultFontSize = fontSize;
            return paragraph;
        }

        /// <summary>
        /// 添加文本内容
        /// </summary>
        public static Paragraph Add(this Paragraph paragraph, string text)
        {
            var span = new Span(text);
            paragraph.Contents.Add(span);
            return paragraph;
        }
    }
}
