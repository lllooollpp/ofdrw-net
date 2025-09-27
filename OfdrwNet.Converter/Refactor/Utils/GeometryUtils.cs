namespace OfdrwNet.Converter.Refactor.Utils;

/// <summary>
/// 几何/单位换算与矩阵工具：集中 PDF pt ↔ mm ↔ 像素(px) 换算，避免魔法常量散落。
/// </summary>
internal static class GeometryUtils
{
    public const double PtPerInch = 72.0;          // PDF 基础：1in = 72pt
    public const double MmPerInch = 25.4;          // 公制换算
    public const double Pt2Mm = MmPerInch / PtPerInch; // 1pt -> mm
    public const double Mm2Pt = PtPerInch / MmPerInch; // 1mm -> pt

    /// <summary>
    /// PDF pt 转 mm
    /// </summary>
    public static double PtToMm(double pt) => pt * Pt2Mm;
    /// <summary>
    /// mm 转 PDF pt
    /// </summary>
    public static double MmToPt(double mm) => mm * Mm2Pt;
    /// <summary>
    /// mm 转像素（基于 DPI）
    /// </summary>
    public static int MmToPx(double mm, double dpi) => (int)System.Math.Round(MmToInch(mm) * dpi);
    /// <summary>
    /// 像素转 mm
    /// </summary>
    public static double PxToMm(int px, double dpi) => InchToMm(px / dpi);

    public static double MmToInch(double mm) => mm / MmPerInch;
    public static double InchToMm(double inch) => inch * MmPerInch;

    /// <summary>
    /// 由 PDF 图像 CTM 生成 OFD 侧使用的 mm 坐标矩阵 (a,b,c,d,e,f) （e,f 为平移）
    /// </summary>
    public static double[] BuildOfdCtmFromPdf(iText.Kernel.Geom.Matrix m)
    {
        // iText 的 Matrix 索引常量在 iText.Kernel.Geom.Matrix 中定义 (I11..I32)
        var a = m.Get(iText.Kernel.Geom.Matrix.I11) * Pt2Mm;
        var b = m.Get(iText.Kernel.Geom.Matrix.I12) * Pt2Mm;
        var c = m.Get(iText.Kernel.Geom.Matrix.I21) * Pt2Mm;
        var d = m.Get(iText.Kernel.Geom.Matrix.I22) * Pt2Mm;
        var e = m.Get(iText.Kernel.Geom.Matrix.I31) * Pt2Mm;
        var f = m.Get(iText.Kernel.Geom.Matrix.I32) * Pt2Mm;
        return new[]{ a,b,c,d,e,f };
    }
}
