namespace OfdrwNet.Core.BasicType;

/// <summary>
/// ST_Box 矩形框类型
/// 对应 Java 版本的 org.ofdrw.core.basicType.ST_Box
/// 用于表示矩形区域的位置和大小
/// </summary>
public class StBox
{
    /// <summary>
    /// 左上角X坐标
    /// </summary>
    public double TopLeftX { get; set; }

    /// <summary>
    /// 左上角Y坐标
    /// </summary>
    public double TopLeftY { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// X坐标（别名，指向TopLeftX）
    /// </summary>
    public double X
    {
        get => TopLeftX;
        set => TopLeftX = value;
    }

    /// <summary>
    /// Y坐标（别名，指向TopLeftY）
    /// </summary>
    public double Y
    {
        get => TopLeftY;
        set => TopLeftY = value;
    }

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public StBox()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="topLeftX">左上角X坐标</param>
    /// <param name="topLeftY">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public StBox(double topLeftX, double topLeftY, double width, double height)
    {
        TopLeftX = topLeftX;
        TopLeftY = topLeftY;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// 从字符串解析矩形框
    /// 格式: "x y width height"
    /// </summary>
    /// <param name="str">字符串表示</param>
    /// <returns>矩形框实例</returns>
    public static StBox Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException("矩形框字符串不能为空");
        }

        string[] parts = str.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            throw new ArgumentException($"矩形框格式错误，应为 'x y width height'，实际为: {str}");
        }

        if (!double.TryParse(parts[0], out double x) ||
            !double.TryParse(parts[1], out double y) ||
            !double.TryParse(parts[2], out double width) ||
            !double.TryParse(parts[3], out double height))
        {
            throw new ArgumentException($"无法解析矩形框数值: {str}");
        }

        return new StBox(x, y, width, height);
    }

    /// <summary>
    /// 获取右下角X坐标
    /// </summary>
    /// <returns>右下角X坐标</returns>
    public double GetBottomRightX()
    {
        return TopLeftX + Width;
    }

    /// <summary>
    /// 获取右下角Y坐标
    /// </summary>
    /// <returns>右下角Y坐标</returns>
    public double GetBottomRightY()
    {
        return TopLeftY + Height;
    }

    /// <summary>
    /// 获取中心点X坐标
    /// </summary>
    /// <returns>中心点X坐标</returns>
    public double GetCenterX()
    {
        return TopLeftX + Width / 2.0;
    }

    /// <summary>
    /// 获取中心点Y坐标
    /// </summary>
    /// <returns>中心点Y坐标</returns>
    public double GetCenterY()
    {
        return TopLeftY + Height / 2.0;
    }

    /// <summary>
    /// 判断是否包含指定点
    /// </summary>
    /// <param name="x">点的X坐标</param>
    /// <param name="y">点的Y坐标</param>
    /// <returns>是否包含</returns>
    public bool Contains(double x, double y)
    {
        return x >= TopLeftX && x <= GetBottomRightX() &&
               y >= TopLeftY && y <= GetBottomRightY();
    }

    /// <summary>
    /// 判断是否与另一个矩形框相交
    /// </summary>
    /// <param name="other">另一个矩形框</param>
    /// <returns>是否相交</returns>
    public bool Intersects(StBox? other)
    {
        if (other == null) return false;

        return !(GetBottomRightX() < other.TopLeftX ||
                 TopLeftX > other.GetBottomRightX() ||
                 GetBottomRightY() < other.TopLeftY ||
                 TopLeftY > other.GetBottomRightY());
    }

    /// <summary>
    /// 判断是否与另一个矩形框相交（别名方法）
    /// </summary>
    /// <param name="other">另一个矩形框</param>
    /// <returns>是否相交</returns>
    public bool IntersectsWith(StBox? other)
    {
        return Intersects(other);
    }

    /// <summary>
    /// 克隆矩形框
    /// </summary>
    /// <returns>克隆的矩形框</returns>
    public StBox Clone()
    {
        return new StBox(TopLeftX, TopLeftY, Width, Height);
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"{TopLeftX} {TopLeftY} {Width} {Height}";
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>是否相等</returns>
    public override bool Equals(object? obj)
    {
        if (obj is StBox other)
        {
            return Math.Abs(TopLeftX - other.TopLeftX) < 1e-9 &&
                   Math.Abs(TopLeftY - other.TopLeftY) < 1e-9 &&
                   Math.Abs(Width - other.Width) < 1e-9 &&
                   Math.Abs(Height - other.Height) < 1e-9;
        }
        return false;
    }

    /// <summary>
    /// 相等性操作符
    /// </summary>
    public static bool operator ==(StBox left, StBox right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// 不相等操作符
    /// </summary>
    public static bool operator !=(StBox left, StBox right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TopLeftX, TopLeftY, Width, Height);
    }

    /// <summary>
    /// 扩展矩形框
    /// </summary>
    /// <param name="deltaX">X方向扩展</param>
    /// <param name="deltaY">Y方向扩展</param>
    /// <param name="deltaWidth">宽度扩展</param>
    /// <param name="deltaHeight">高度扩展</param>
    /// <returns>新的矩形框</returns>
    public StBox Expand(double deltaX, double deltaY, double deltaWidth, double deltaHeight)
    {
        return new StBox(
            TopLeftX + deltaX,
            TopLeftY + deltaY,
            Width + deltaWidth,
            Height + deltaHeight
        );
    }

    /// <summary>
    /// 计算与另一个矩形框的并集
    /// </summary>
    /// <param name="other">另一个矩形框</param>
    /// <returns>并集矩形框</returns>
    public StBox Union(StBox? other)
    {
        if (other == null) return Clone();

        double minX = Math.Min(TopLeftX, other.TopLeftX);
        double minY = Math.Min(TopLeftY, other.TopLeftY);
        double maxX = Math.Max(GetBottomRightX(), other.GetBottomRightX());
        double maxY = Math.Max(GetBottomRightY(), other.GetBottomRightY());

        return new StBox(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 计算与另一个矩形框的交集
    /// </summary>
    /// <param name="other">另一个矩形框</param>
    /// <returns>交集矩形框，如果不相交则返回null</returns>
    public StBox? Intersection(StBox? other)
    {
        if (other == null || !Intersects(other)) return null;

        double minX = Math.Max(TopLeftX, other.TopLeftX);
        double minY = Math.Max(TopLeftY, other.TopLeftY);
        double maxX = Math.Min(GetBottomRightX(), other.GetBottomRightX());
        double maxY = Math.Min(GetBottomRightY(), other.GetBottomRightY());

        return new StBox(minX, minY, maxX - minX, maxY - minY);
    }
}