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

    /// <summary>
    /// 返回单位变换矩阵（单位CTM）
    /// </summary>
    public static StArray UnitCTM()
    {
        return new StArray(1, 0, 0, 1, 0, 0);
    }

    /// <summary>
    /// 克隆数组（深拷贝）
    /// </summary>
    /// <returns>新的 StArray 实例</returns>
    public StArray Clone()
    {
        return new StArray((double[])Values.Clone());
    }

    /// <summary>
    /// 以当前矩阵作为左矩阵，与参数矩阵相乘（即 this * other），返回乘积矩阵
    /// 矩阵按照 OFD/ST_Array 约定 [a b c d e f] -> [[a, c, e],[b, d, f],[0,0,1]]
    /// </summary>
    /// <param name="other">右矩阵（可为 null，null 等价于单位矩阵）</param>
    /// <returns>乘积矩阵</returns>
    public StArray MtxMul(StArray? other)
    {
        if (other == null)
            return this.Clone();

        if (Values.Length < 6 || other.Values.Length < 6)
            throw new ArgumentException("矩阵必须包含至少6个元素", nameof(other));

        var a1 = Values[0]; var b1 = Values[1]; var c1 = Values[2]; var d1 = Values[3]; var e1 = Values[4]; var f1 = Values[5];
        var a2 = other.Values[0]; var b2 = other.Values[1]; var c2 = other.Values[2]; var d2 = other.Values[3]; var e2 = other.Values[4]; var f2 = other.Values[5];

        var a = a1 * a2 + c1 * b2;
        var b = b1 * a2 + d1 * b2;
        var c = a1 * c2 + c1 * d2;
        var d = b1 * c2 + d1 * d2;
        var e = a1 * e2 + c1 * f2 + e1;
        var f = b1 * e2 + d1 * f2 + f1;

        return new StArray(a, b, c, d, e, f);
    }
}
