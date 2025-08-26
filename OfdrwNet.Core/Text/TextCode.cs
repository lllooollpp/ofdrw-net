using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using System.Text;

namespace OfdrwNet.Core.Text;

/// <summary>
/// 文字定位
/// 
/// 文字对象使用严格的文字定位信息进行定位
/// 
/// 11.3 文字定位 图 61 表 46
/// 
/// 对应 Java 版本的 org.ofdrw.core.text.TextCode
/// </summary>
public class TextCode : OfdElement
{
    /// <summary>
    /// 从现有元素构造文字定位
    /// </summary>
    /// <param name="element">XML元素</param>
    public TextCode(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文字定位元素
    /// </summary>
    public TextCode() : base("TextCode")
    {
    }

    /// <summary>
    /// 设置文字内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <returns>this</returns>
    public TextCode SetContent(string content)
    {
        Element.Value = content;
        return this;
    }

    /// <summary>
    /// 获取文字内容
    /// </summary>
    /// <returns>文字内容</returns>
    public string GetContent()
    {
        return Element.Value;
    }

    /// <summary>
    /// 设置坐标
    /// </summary>
    /// <param name="x">横坐标</param>
    /// <param name="y">纵坐标</param>
    /// <returns>this</returns>
    public TextCode SetCoordinate(double x, double y)
    {
        return SetX(x).SetY(y);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置第一个文字的字形在对象坐标系下的 X 坐标
    /// 当 X 不出现，则采用上一个 TextCode 的 X 值，文字对象中的第一个
    /// TextCode 的属性必选
    /// </summary>
    /// <param name="x">第一个文字的字形在对象坐标系下的 X 坐标</param>
    /// <returns>this</returns>
    public TextCode SetX(double? x)
    {
        if (x == null)
        {
            RemoveAttribute("X");
            return this;
        }
        AddAttribute("X", FormatDouble(x.Value));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取第一个文字的字形在对象坐标系下的 X 坐标
    /// 当 X 不出现，则采用上一个 TextCode 的 X 值，文字对象中的第一个
    /// TextCode 的属性必选
    /// </summary>
    /// <returns>第一个文字的字形在对象坐标系下的 X 坐标；null表示采用上一个 TextCode 的 X 值</returns>
    public double? GetX()
    {
        var str = GetAttributeValue("X");
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        return double.TryParse(str, out double result) ? result : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置第一个文字的字形原点在对象坐标系下的 Y 坐标
    /// 当 Y 不出现，则采用上一个 TextCode 的 Y 值，文字对象中的第一个
    /// TextCode 的属性必选
    /// </summary>
    /// <param name="y">第一个文字的字形原点在对象坐标系下的 Y 坐标</param>
    /// <returns>this</returns>
    public TextCode SetY(double? y)
    {
        if (y == null)
        {
            RemoveAttribute("Y");
            return this;
        }
        AddAttribute("Y", FormatDouble(y.Value));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取第一个文字的字形在对象坐标系下的 Y 坐标
    /// 当 Y 不出现，则采用上一个 TextCode 的 Y 值，文字对象中的第一个
    /// TextCode 的属性必选
    /// </summary>
    /// <returns>第一个文字的字形在对象坐标系下的 Y 坐标；null表示采用上一个 TextCode 的 Y 值</returns>
    public double? GetY()
    {
        var str = GetAttributeValue("Y");
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        return double.TryParse(str, out double result) ? result : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置文字之间在 X 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 X 方向的偏移值
    /// DeltaX 不出现时，表示文字的绘制点在 X 方向不做偏移。
    /// </summary>
    /// <param name="deltaX">文字之间在 X 方向上的偏移值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaX(StArray? deltaX)
    {
        if (deltaX == null)
        {
            RemoveAttribute("DeltaX");
            return this;
        }
        AddAttribute("DeltaX", deltaX.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置文字之间在 X 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 X 方向的偏移值
    /// DeltaX 不出现时，表示文字的绘制点在 X 方向不做偏移。
    /// </summary>
    /// <param name="arr">文字之间在 X 方向上的偏移值数值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaX(params double[] arr)
    {
        return SetDeltaX(new StArray(arr));
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取文字之间在 X 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 X 方向的偏移值
    /// DeltaX 不出现时，表示文字的绘制点在 X 方向不做偏移。
    /// </summary>
    /// <returns>文字之间在 X 方向上的偏移值；null表示不偏移</returns>
    public StArray? GetDeltaX()
    {
        var str = GetAttributeValue("DeltaX");
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        return StArray.Parse(DeltaFormatter(str));
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置文字之间在 Y 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 Y 方向的偏移值
    /// DeltaY 不出现时，表示文字的绘制点在 Y 方向不做偏移。
    /// </summary>
    /// <param name="deltaY">文字之间在 Y 方向上的偏移值；null表示不偏移</param>
    /// <returns>this</returns>
    public TextCode SetDeltaY(StArray? deltaY)
    {
        if (deltaY == null)
        {
            RemoveAttribute("DeltaY");
            return this;
        }
        AddAttribute("DeltaY", deltaY.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置文字之间在 Y 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 Y 方向的偏移值
    /// DeltaY 不出现时，表示文字的绘制点在 Y 方向不做偏移。
    /// </summary>
    /// <param name="arr">文字之间在 Y 方向上的偏移数值</param>
    /// <returns>this</returns>
    public TextCode SetDeltaY(params double[] arr)
    {
        return SetDeltaY(new StArray(arr));
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取文字之间在 Y 方向上的偏移值
    /// double 型数值队列，列表中的每个值代表一个文字与前一个
    /// 文字之间在 Y 方向的偏移值
    /// DeltaY 不出现时，表示文字的绘制点在 Y 方向不做偏移。
    /// </summary>
    /// <returns>文字之间在 Y 方向上的偏移值；null表示不偏移</returns>
    public StArray? GetDeltaY()
    {
        var str = GetAttributeValue("DeltaY");
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        return StArray.Parse(DeltaFormatter(str));
    }

    /// <summary>
    /// 解析delta的值，处理g的格式
    /// </summary>
    /// <param name="delta">delta字符串</param>
    /// <returns>格式化后的字符串</returns>
    private string DeltaFormatter(string delta)
    {
        if (!delta.Contains("g"))
        {
            return delta;
        }

        var parts = delta.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var floatList = new List<string>();

        bool gFlag = false;
        bool gProcessing = false;
        int gItemCount = 0;

        foreach (var part in parts)
        {
            if (part == "g")
            {
                gFlag = true;
            }
            else
            {
                if (gFlag)
                {
                    gItemCount = int.Parse(part);
                    gProcessing = true;
                    gFlag = false;
                }
                else if (gProcessing)
                {
                    for (int j = 0; j < gItemCount; j++)
                    {
                        floatList.Add(part);
                    }
                    gProcessing = false;
                }
                else
                {
                    floatList.Add(part);
                }
            }
        }

        return string.Join(" ", floatList);
    }

    /// <summary>
    /// 格式化double值
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>格式化字符串</returns>
    private static string FormatDouble(double value)
    {
        return value.ToString("0.######");
    }
}
