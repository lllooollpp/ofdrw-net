namespace OfdrwNet.Utils;

internal static class CtmUtil
{
    public static double[]? Normalize(double[]? ctm)
    {
        if (ctm == null || ctm.Length != 6) return null;
        if (Math.Abs(ctm[0] - 1) < 1e-9 && Math.Abs(ctm[1]) < 1e-9 && Math.Abs(ctm[2]) < 1e-9 && Math.Abs(ctm[3] - 1) < 1e-9 && Math.Abs(ctm[4]) < 1e-9 && Math.Abs(ctm[5]) < 1e-9)
            return null;
        return ctm.ToArray();
    }

    public static string Format(double[] ctm) => string.Join(" ", ctm.Select(v => v.ToString("0.########")));
}
