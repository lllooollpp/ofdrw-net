using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph;

/// <summary>
/// 虚线模式
/// 
/// 描述虚线样式，通过一组数值定义虚线中实线和空隙的长度。
/// 数组中的值交替表示实线段和空隙段的长度，单位为毫米。
/// 
/// 对应Java版本：org.ofdrw.core.pageDescription.drawParam.DashPattern
/// 表 27 DashPattern
/// </summary>
public class DashPattern : StArray
{
    /// <summary>
    /// 构造虚线模式
    /// </summary>
    /// <param name="pattern">虚线模式数组，交替表示实线段和空隙段的长度</param>
    public DashPattern(params double[] pattern) : base(pattern)
    {
        if (pattern == null || pattern.Length == 0)
        {
            throw new ArgumentException("虚线模式不能为空", nameof(pattern));
        }

        // 确保所有值都是正数
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] <= 0)
            {
                throw new ArgumentException($"虚线模式值必须大于0，位置{i}的值为{pattern[i]}", nameof(pattern));
            }
        }
    }

    /// <summary>
    /// 从字符串解析虚线模式
    /// </summary>
    /// <param name="str">字符串表示，用空格分隔各段长度</param>
    /// <returns>虚线模式</returns>
    public static new DashPattern Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("虚线模式字符串不能为空", nameof(str));
        }

        var parts = str.Trim().Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var pattern = new double[parts.Length];
        
        for (int i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], out pattern[i]))
            {
                throw new ArgumentException($"无法解析虚线模式值: {parts[i]}", nameof(str));
            }
        }

        return new DashPattern(pattern);
    }

    /// <summary>
    /// 创建实线模式（空数组）
    /// </summary>
    /// <returns>实线模式</returns>
    public static DashPattern Solid()
    {
        return new DashPattern(1.0); // 至少需要一个值
    }

    /// <summary>
    /// 创建简单虚线模式（等长虚实线）
    /// </summary>
    /// <param name="dashLength">虚线段长度</param>
    /// <param name="gapLength">空隙段长度，默认与虚线段相等</param>
    /// <returns>虚线模式</returns>
    public static DashPattern SimpleDash(double dashLength, double gapLength = -1)
    {
        if (dashLength <= 0)
            throw new ArgumentException("虚线段长度必须大于0", nameof(dashLength));
            
        if (gapLength <= 0)
            gapLength = dashLength;
            
        return new DashPattern(dashLength, gapLength);
    }

    /// <summary>
    /// 创建点划线模式
    /// </summary>
    /// <param name="dashLength">长划线段长度</param>
    /// <param name="dotLength">点长度</param>
    /// <param name="gapLength">间隙长度</param>
    /// <returns>点划线模式</returns>
    public static DashPattern DashDot(double dashLength, double dotLength, double gapLength)
    {
        if (dashLength <= 0 || dotLength <= 0 || gapLength <= 0)
            throw new ArgumentException("所有长度值必须大于0");
            
        return new DashPattern(dashLength, gapLength, dotLength, gapLength);
    }

    /// <summary>
    /// 获取虚线模式数组
    /// </summary>
    /// <returns>虚线模式数组</returns>
    public double[] GetPattern()
    {
        return ToArray();
    }

    /// <summary>
    /// 判断是否为实线
    /// </summary>
    /// <returns>是否为实线</returns>
    public bool IsSolid()
    {
        var pattern = GetPattern();
        return pattern.Length <= 1;
    }

    /// <summary>
    /// 获取模式的总长度（一个完整周期）
    /// </summary>
    /// <returns>模式总长度</returns>
    public double GetPatternLength()
    {
        return GetPattern().Sum();
    }

    /// <summary>
    /// 克隆虚线模式
    /// </summary>
    /// <returns>克隆的虚线模式</returns>
    public DashPattern Clone()
    {
        return new DashPattern(GetPattern());
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        var pattern = GetPattern();
        return string.Join(" ", pattern.Select(x => x.ToString("0.###")));
    }

    /// <summary>
    /// 验证虚线模式是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        try
        {
            var pattern = GetPattern();
            return pattern.Length > 0 && pattern.All(x => x > 0);
        }
        catch
        {
            return false;
        }
    }
}
