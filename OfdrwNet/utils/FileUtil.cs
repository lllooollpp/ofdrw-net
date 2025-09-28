using System.Text;

namespace OfdrwNet.Utils;

internal static class FileUtil
{
    public static async Task WriteTextFileUtf8LfAsync(string path, string content)
    {
        content ??= string.Empty;
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes(content)).ConfigureAwait(false);
    }
}
