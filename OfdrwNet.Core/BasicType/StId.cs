namespace OfdrwNet.Core.BasicType;

/// <summary>
/// ST_ID 标识符类型
/// 对应 Java 版本的 org.ofdrw.core.basicType.ST_ID
/// </summary>
public class StId
{
    /// <summary>
    /// 标识符值
    /// </summary>
    public long Value { get; set; }

    /// <summary>
    /// 初始化标识符
    /// </summary>
    /// <param name="value">标识符值</param>
    public StId(long value)
    {
        Value = value;
    }

    /// <summary>
    /// 从字符串解析标识符
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>标识符实例</returns>
    public static StId Parse(string str)
    {
        if (long.TryParse(str, out long value))
        {
            return new StId(value);
        }
        throw new ArgumentException($"无法解析标识符: {str}");
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>标识符的字符串表示</returns>
    public override string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// 获取引用字符串（添加引用前缀）
    /// </summary>
    /// <returns>引用字符串</returns>
    public string ToReference()
    {
        return Value.ToString();
    }

    /// <summary>
    /// 隐式转换为 long
    /// </summary>
    /// <param name="stId">标识符实例</param>
    /// <returns>长整型值</returns>
    public static implicit operator long(StId stId)
    {
        return stId.Value;
    }

    /// <summary>
    /// 隐式转换从 long
    /// </summary>
    /// <param name="value">长整型值</param>
    /// <returns>标识符实例</returns>
    public static implicit operator StId(long value)
    {
        return new StId(value);
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StId other)
            return Value == other.Value;
        if (obj is long longValue)
            return Value == longValue;
        return false;
    }

    /// <summary>
    /// 相等性操作符
    /// </summary>
    public static bool operator ==(StId left, StId right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Value == right.Value;
    }

    /// <summary>
    /// 不相等操作符
    /// </summary>
    public static bool operator !=(StId left, StId right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}