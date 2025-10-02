namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 边界框
/// </summary>
public sealed class BoundingBox
{
    /// <summary>
    /// 左上角 X 坐标
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// 左上角 Y 坐标
    /// </summary>
    public required double Y { get; init; }

    /// <summary>
    /// 宽度
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// 高度
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// 计算两个边界框的 IoU（交并比）
    /// </summary>
    public double ComputeIoU(BoundingBox other)
    {
        var intersectX = Math.Max(X, other.X);
        var intersectY = Math.Max(Y, other.Y);
        var intersectRight = Math.Min(X + Width, other.X + other.Width);
        var intersectBottom = Math.Min(Y + Height, other.Y + other.Height);

        if (intersectRight <= intersectX || intersectBottom <= intersectY)
        {
            return 0.0;
        }

        var intersectArea = (intersectRight - intersectX) * (intersectBottom - intersectY);
        var unionArea = Width * Height + other.Width * other.Height - intersectArea;

        return unionArea > 0 ? intersectArea / unionArea : 0.0;
    }
}
