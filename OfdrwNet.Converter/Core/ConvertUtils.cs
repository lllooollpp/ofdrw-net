using System;
using System.IO;

namespace OfdrwNet.Converter.Core;

/// <summary>
/// 转换工具类
/// 提供转换过程中需要的通用工具方法
/// </summary>
public static class ConvertUtils
{
    /// <summary>
    /// 规范化输入为临时 OFD 文件
    /// </summary>
    /// <param name="input">输入对象（Stream 或 string 文件路径）</param>
    /// <returns>OFD 文件路径和可能的临时文件路径</returns>
    public static (string ofdPath, string? tempFile) NormalizeInputToTempOfd(object input)
    {
        switch (input)
        {
            case string path when File.Exists(path):
                return (path, null);
            case Stream stream:
                string tempOfd = Path.ChangeExtension(Path.GetTempFileName(), ".ofd");
                using (var fs = File.Create(tempOfd))
                {
                    stream.CopyTo(fs);
                }
                return (tempOfd, tempOfd);
            default:
                throw new ArgumentException("不支持的输入格式(input)，仅支持 Stream、string 文件路径");
        }
    }

    /// <summary>
    /// 规范化输出路径
    /// </summary>
    /// <param name="output">输出对象（string 文件路径）</param>
    /// <returns>输出文件路径，如果是 Stream 则返回 null</returns>
    public static string? NormalizeOutputPath(object output)
    {
        if (output is string s)
            return s;
        return null; // Stream 情况由调用处处理
    }

    /// <summary>
    /// 安全删除文件
    /// </summary>
    /// <param name="path">要删除的文件路径</param>
    public static void SafeDelete(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { /* ignore */ }
    }

    /// <summary>
    /// 字体归一逻辑（向后兼容的内部代理，后续可删除）
    /// </summary>
    internal static string NormalizeLogicalFontName(string baseName)
    {
        return Refactor.Utils.FontUtils.NormalizeLogicalFontName(baseName);
    }

    /// <summary>
    /// 查找系统字体路径（向后兼容的内部代理，后续可删除）
    /// </summary>
    private static string? FindSystemFontPath(string logical)
    {
        return Refactor.Utils.FontUtils.FindSystemFontPath(logical);
    }
}
