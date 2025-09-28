namespace OfdrwNet.Services;

/// <summary>
/// 资源生成结果聚合
/// </summary>
internal sealed class ResourceGenerationResult
{
    public string PublicResRelativePath { get; init; } = string.Empty;
    public string DocumentResRelativePath { get; init; } = string.Empty;
    public int MaxUnitIdAfterResources { get; init; }
}
