using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OfdrwNet.Converter.Extensions;

/// <summary>
/// 附件添加器。
/// </summary>
/// <remarks>
/// 嵌入外部文件到 OFD 文档。
/// FR-30: 附件管理
///
/// 功能:
/// - 添加任意格式文件作为附件
/// - 计算附件 SHA-256 哈希
/// - 生成附件清单
/// - 验证附件完整性
///
/// 附件存储:
/// - Attachments/ 目录
/// - 命名格式: {hash}.{ext}
/// - 元数据: attachments.json
/// </remarks>
public sealed class AttachmentAdder
{
    private readonly ILogger<AttachmentAdder> _logger;
    private readonly MetadataService _metadataService;
    private readonly List<AttachmentInfo> _attachments;

    /// <summary>
    /// 最大附件大小(100MB)。
    /// </summary>
    private const long _maxAttachmentSize = 100 * 1024 * 1024;

    /// <summary>
    /// 初始化 AttachmentAdder 实例。
    /// </summary>
    public AttachmentAdder(
        ILogger<AttachmentAdder> logger,
        MetadataService metadataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _attachments = new List<AttachmentInfo>();
    }

    /// <summary>
    /// 添加附件。
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="displayName">显示名称(可选,默认使用文件名)</param>
    /// <param name="description">附件描述(可选)</param>
    /// <returns>附件信息</returns>
    public AttachmentInfo AddAttachment(string filePath, string? displayName = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Attachment file not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);

        // 检查文件大小
        if (fileInfo.Length > _maxAttachmentSize)
        {
            throw new InvalidOperationException(
                $"Attachment size ({fileInfo.Length} bytes) exceeds maximum allowed size ({_maxAttachmentSize} bytes)");
        }

        // 计算哈希
        var hash = ComputeFileHash(filePath);

        // 检查重复
        var existing = _attachments.FirstOrDefault(a => a.Hash == hash);
        if (existing != null)
        {
            _logger.LogWarning("Attachment already exists (hash: {Hash}), skipping duplicate", hash);
            return existing;
        }

        var attachment = new AttachmentInfo
        {
            OriginalPath = filePath,
            DisplayName = displayName ?? Path.GetFileName(filePath),
            FileName = Path.GetFileName(filePath),
            Extension = fileInfo.Extension,
            SizeBytes = fileInfo.Length,
            Hash = hash,
            Description = description,
            AddedAt = DateTime.UtcNow
        };

        _attachments.Add(attachment);
        _logger.LogInformation("Added attachment: {Name} ({Size} bytes, Hash: {Hash})",
            attachment.DisplayName, attachment.SizeBytes, attachment.Hash);

        // 更新元数据
        UpdateMetadata();

        return attachment;
    }

    /// <summary>
    /// 批量添加附件。
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <returns>成功添加的附件信息列表</returns>
    public IList<AttachmentInfo> AddAttachments(IEnumerable<string> filePaths)
    {
        if (filePaths == null)
        {
            throw new ArgumentNullException(nameof(filePaths));
        }

        var added = new List<AttachmentInfo>();

        foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            try
            {
                var attachment = AddAttachment(path);
                added.Add(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add attachment: {Path}", path);
            }
        }

        return added;
    }

    /// <summary>
    /// 移除附件。
    /// </summary>
    /// <param name="hash">附件哈希</param>
    /// <returns>移除成功返回 true</returns>
    public bool RemoveAttachment(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        var removed = _attachments.RemoveAll(a => a.Hash == hash);
        if (removed > 0)
        {
            _logger.LogInformation("Removed {Count} attachment(s) with hash: {Hash}", removed, hash);
            UpdateMetadata();
        }

        return removed > 0;
    }

    /// <summary>
    /// 获取所有附件。
    /// </summary>
    public IReadOnlyList<AttachmentInfo> GetAttachments()
    {
        return _attachments.AsReadOnly();
    }

    /// <summary>
    /// 获取附件总大小。
    /// </summary>
    public long GetTotalSize()
    {
        return _attachments.Sum(a => a.SizeBytes);
    }

    /// <summary>
    /// 生成附件清单。
    /// </summary>
    public AttachmentManifest GenerateManifest()
    {
        return new AttachmentManifest
        {
            Count = _attachments.Count,
            TotalSizeBytes = GetTotalSize(),
            Attachments = _attachments.Select(a => (object)new
            {
                a.DisplayName,
                a.FileName,
                a.Extension,
                a.SizeBytes,
                a.Hash,
                a.Description,
                AddedAt = a.AddedAt.ToString("O")
            }).ToList()
        };
    }

    /// <summary>
    /// 验证附件完整性。
    /// </summary>
    /// <param name="attachment">附件信息</param>
    /// <returns>验证成功返回 true</returns>
    public bool VerifyIntegrity(AttachmentInfo attachment)
    {
        if (attachment == null)
        {
            throw new ArgumentNullException(nameof(attachment));
        }

        if (!File.Exists(attachment.OriginalPath))
        {
            _logger.LogError("Attachment file not found: {Path}", attachment.OriginalPath);
            return false;
        }

        var currentHash = ComputeFileHash(attachment.OriginalPath);
        var isValid = currentHash == attachment.Hash;

        _logger.LogDebug("Attachment integrity check: {Name} = {Result}",
            attachment.DisplayName, isValid ? "Valid" : "Invalid");

        return isValid;
    }

    /// <summary>
    /// 清除所有附件。
    /// </summary>
    public void Clear()
    {
        _logger.LogInformation("Clearing all attachments (Count: {Count})", _attachments.Count);
        _attachments.Clear();
        UpdateMetadata();
    }

    /// <summary>
    /// 计算文件 SHA-256 哈希。
    /// </summary>
    private string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 更新元数据服务。
    /// </summary>
    private void UpdateMetadata()
    {
        _metadataService.AddCustomData("attachments.count", _attachments.Count);
        _metadataService.AddCustomData("attachments.total_size", GetTotalSize());

        if (_attachments.Count > 0)
        {
            _metadataService.AddTag("has-attachments");
        }
    }
}

/// <summary>
/// 附件信息。
/// </summary>
public sealed class AttachmentInfo
{
    /// <summary>
    /// 原始文件路径。
    /// </summary>
    public string OriginalPath { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 文件名。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名。
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小(字节)。
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// SHA-256 哈希。
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// 附件描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 添加时间。
    /// </summary>
    public DateTime AddedAt { get; set; }
}

/// <summary>
/// 附件清单。
/// </summary>
public sealed class AttachmentManifest
{
    /// <summary>
    /// 附件数量。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 总大小(字节)。
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// 附件列表。
    /// </summary>
    public List<object> Attachments { get; set; } = new();
}
