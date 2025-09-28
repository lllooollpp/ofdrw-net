namespace OfdrwNet.Models;

/// <summary>
/// 原始图片资源(页面对象引用)
/// </summary>
internal class RawImage
{
    public string Format { get; set; } = "PNG"; // PNG/JPG/GIF/BMP/TIFF/WEBP
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int Page { get; set; } = 1;
    public int ResourceID { get; set; }
    public string Hash { get; set; } = string.Empty; // SHA256
    public bool IsFirstResource { get; set; }
    public double[]? CTM { get; set; }
    public int Sequence { get; set; }
    public int Z { get; set; }
    public int Alpha { get; set; } = 255;
    public string? AltText { get; set; }
}
