namespace OfdrwNet.Models;

/// <summary>
/// 原始矢量路径
/// </summary>
internal class RawPath
{
    public string PathData { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Page { get; set; } = 1;
    public double[]? CTM { get; set; }
    public string? StrokeColor { get; set; }
    public string? FillColor { get; set; }
    public double? LineWidth { get; set; }
    public bool? Stroke { get; set; }
    public bool? Fill { get; set; }
}
