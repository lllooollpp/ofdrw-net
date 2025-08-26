namespace OfdrwNet.Core.BasicType;

/// <summary>
/// ST_Loc 位置类型
/// 对应 Java 版本的 org.ofdrw.core.basicType.ST_Loc
/// 用于表示文件路径或资源位置
/// </summary>
public class StLoc
{
    /// <summary>
    /// 位置路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 初始化位置
    /// </summary>
    /// <param name="path">路径字符串</param>
    public StLoc(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>
    /// 连接路径
    /// </summary>
    /// <param name="subPath">子路径</param>
    /// <returns>新的位置实例</returns>
    public StLoc Cat(string subPath)
    {
        if (string.IsNullOrEmpty(subPath))
            return this;
        
        string separator = Path.EndsWith("/") || subPath.StartsWith("/") ? "" : "/";
        return new StLoc($"{Path}{separator}{subPath}");
    }

    /// <summary>
    /// 获取父级路径
    /// </summary>
    /// <returns>父级位置，如果没有父级则返回当前位置</returns>
    public StLoc Parent()
    {
        int lastSlash = Path.LastIndexOf('/');
        if (lastSlash > 0)
        {
            return new StLoc(Path.Substring(0, lastSlash));
        }
        return new StLoc("/");
    }

    /// <summary>
    /// 获取文件名
    /// </summary>
    /// <returns>文件名</returns>
    public string GetFileName()
    {
        int lastSlash = Path.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < Path.Length - 1)
        {
            return Path.Substring(lastSlash + 1);
        }
        return Path;
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>路径字符串</returns>
    public override string ToString()
    {
        return Path;
    }

    /// <summary>
    /// 隐式转换为字符串
    /// </summary>
    /// <param name="stLoc">位置实例</param>
    /// <returns>路径字符串</returns>
    public static implicit operator string(StLoc stLoc)
    {
        return stLoc.Path;
    }

    /// <summary>
    /// 隐式转换从字符串
    /// </summary>
    /// <param name="path">路径字符串</param>
    /// <returns>位置实例</returns>
    public static implicit operator StLoc(string path)
    {
        return new StLoc(path);
    }

    /// <summary>
    /// 从字符串解析位置
    /// </summary>
    /// <param name="path">路径字符串</param>
    /// <returns>位置实例</returns>
    public static StLoc Parse(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("路径不能为空", nameof(path));
        return new StLoc(path);
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StLoc other)
            return Path == other.Path;
        if (obj is string stringValue)
            return Path == stringValue;
        return false;
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return Path.GetHashCode();
    }
}