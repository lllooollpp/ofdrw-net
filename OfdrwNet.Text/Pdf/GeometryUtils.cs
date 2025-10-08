namespace OfdrwNet.Text.Pdf;

/// <summary>
/// 几何与单位换算工具，整合 PDF pt ↔ mm 的基本操作。
/// </summary>
internal static class GeometryUtils
{
    public const double PtPerInch = 72.0;
    public const double MmPerInch = 25.4;
    public const double Pt2Mm = MmPerInch / PtPerInch;
    public const double Mm2Pt = PtPerInch / MmPerInch;

    public static double PtToMm(double pt) => pt * Pt2Mm;
    public static double MmToPt(double mm) => mm * Mm2Pt;
    public static int MmToPx(double mm, double dpi) => (int)System.Math.Round(MmToInch(mm) * dpi);
    public static double PxToMm(int px, double dpi) => InchToMm(px / dpi);

    public static double MmToInch(double mm) => mm / MmPerInch;
    public static double InchToMm(double inch) => inch * MmPerInch;

    /// <summary>
    /// 将 PDF CTM (pt) 转换为 OFD 坐标（mm）。
    /// </summary>
    public static double[] BuildOfdCtmFromPdf(iText.Kernel.Geom.Matrix m)
    {
        var a = m.Get(iText.Kernel.Geom.Matrix.I11) * Pt2Mm;
        var b = m.Get(iText.Kernel.Geom.Matrix.I12) * Pt2Mm;
        var c = m.Get(iText.Kernel.Geom.Matrix.I21) * Pt2Mm;
        var d = m.Get(iText.Kernel.Geom.Matrix.I22) * Pt2Mm;
        var e = m.Get(iText.Kernel.Geom.Matrix.I31) * Pt2Mm;
        var f = m.Get(iText.Kernel.Geom.Matrix.I32) * Pt2Mm;
        return new[] { a, b, c, d, e, f };
    }
}
