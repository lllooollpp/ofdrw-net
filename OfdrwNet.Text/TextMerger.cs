using OfdrwNet.Core;
using System.Text;

namespace OfdrwNet.Text;

/// <summary>
/// 文本合并器，负责将文本块合并为连续文本
/// </summary>
public class TextMerger : ITextMerger
{
    /// <summary>
    /// 合并文本块为连续文本
    /// </summary>
    /// <param name="textBlocks">文本块列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合并后的文本</returns>
    public async Task<string> MergeTextBlocksAsync(IEnumerable<ITextBlock> textBlocks, CancellationToken cancellationToken = default)
    {
        var textBlockList = textBlocks.ToList();
        if (textBlockList.Count == 0)
            return string.Empty;

        var textBuilder = new StringBuilder();
        var sortedBlocks = textBlockList.OrderBy(b => b.Y).ToList();

        var currentY = sortedBlocks[0].Y;
        var lineHeight = sortedBlocks[0].FontSize * 1.2f; // 估算行高
        var currentLine = new StringBuilder();

        foreach (var block in sortedBlocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 如果Y坐标变化超过行高，认为是新行
            if (Math.Abs(block.Y - currentY) > lineHeight / 2)
            {
                if (currentLine.Length > 0)
                {
                    textBuilder.AppendLine(currentLine.ToString().Trim());
                    currentLine.Clear();
                }
                currentY = block.Y;
            }

            // 添加文本到当前行
            if (currentLine.Length > 0 && !block.Content.StartsWith(" "))
            {
                currentLine.Append(" ");
            }
            currentLine.Append(block.Content);
        }

        // 添加最后一行
        if (currentLine.Length > 0)
        {
            textBuilder.AppendLine(currentLine.ToString().Trim());
        }

        await Task.CompletedTask;
        return textBuilder.ToString();
    }
}
