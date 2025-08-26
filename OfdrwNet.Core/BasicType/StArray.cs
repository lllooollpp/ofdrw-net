namespace OfdrwNet.Core.BasicType;

/// <summary>
/// ST_Array 数组类型
/// 对应 Java 版本的 org.ofdrw.core.basicType.ST_Array
/// 用于表示数字数组，通常用空格分隔
/// </summary>
public class StArray
{
    /// <summary>
    /// 数组值
    /// </summary>
    public double[] Values { get; set; }

    /// <summary>
    /// 初始化数组
    /// </summary>
    /// <param name="values">数组值</param>
    public StArray(params double[] values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// 初始化数组（整数重载）
    /// </summary>
    /// <param name="values">数组值</param>
    public StArray(params int[] values)
    {
        Values = values?.Select(x => (double)x).ToArray() ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// 从字符串解析数组
    /// </summary>
    /// <param name="str">字符串表示，数字之间用空格分隔</param>
    /// <returns>数组实例</returns>
    public static StArray Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            throw new ArgumentException("数组字符串不能为空", nameof(str));

        var parts = str.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var values = new double[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], out values[i]))
            {
                throw new ArgumentException($"无法解析数组元素: {parts[i]}");
            }
        }

        return new StArray(values);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>数组的字符串表示，数字之间用空格分隔</returns>
    public override string ToString()
    {
        return string.Join(" ", Values.Select(v => v.ToString("0.######")));
    }

    /// <summary>
    /// 获取数组长度
    /// </summary>
    /// <returns>数组长度</returns>
    public int Length => Values.Length;

    /// <summary>
    /// 索引器
    /// </summary>
    /// <param name="index">索引</param>
    /// <returns>指定位置的值</returns>
    public double this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    /// <param name="array">数组实例</param>
    /// <returns>字符串表示</returns>
    public static implicit operator string(StArray array)
    {
        return array.ToString();
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StArray other)
        {
            if (Values.Length != other.Values.Length)
                return false;
            
            for (int i = 0; i < Values.Length; i++)
            {
                if (Math.Abs(Values[i] - other.Values[i]) > 1e-10)
                    return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in Values)
        {
            hash.Add(value);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// 转换为double数组
    /// </summary>
    /// <returns>double数组</returns>
    public double[] ToArray()
    {
        return (double[])Values.Clone();
    }

    /// <summary>
    /// 转换为int数组
    /// </summary>
    /// <returns>int数组</returns>
    public int[] ToIntArray()
    {
        return Values.Select(x => (int)Math.Round(x)).ToArray();
    }
}
