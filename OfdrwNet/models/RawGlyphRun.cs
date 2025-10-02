namespace OfdrwNet.Models;

/// <summary>
/// 原始字形运行（低级文本对象）
/// </summary>
internal class RawGlyphRun
{
    public string FontName { get; set; } = "SimSun";
    public double FontSizeMm { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Text { get; set; } = string.Empty;
    public double[]? DeltaX { get; set; }
    public double[]? DeltaY { get; set; }
    public int[]? Glyphs { get; set; }
    public int Page { get; set; } = 1;
    public double[]? CTM { get; set; } // a b c d e f
    public double? BaselineY { get; set; }
    public double[]? CharStarts { get; set; }
    public double[]? CharAdvances { get; set; }
}
