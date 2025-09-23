using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 文档加载器接口，处理OFD文档的加载和初始化
    /// </summary>
    public interface IDocumentLoader
    {
        /// <summary>
        /// 异步加载OFD文档
        /// </summary>
        /// <param name="source">文档源 (文件路径、流或目录)</param>
        /// <param name="options">加载选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>已加载的OFD文档对象</returns>
        Task<OfdDocument> LoadDocumentAsync(
            DocumentSource source,
            LoadOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证OFD文档结构和格式
        /// </summary>
        /// <param name="source">文档源</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateDocumentAsync(DocumentSource source);

        /// <summary>
        /// 获取文档基本信息而不完全加载
        /// </summary>
        /// <param name="source">文档源</param>
        /// <returns>文档元数据</returns>
        Task<DocumentMetadata> GetDocumentInfoAsync(DocumentSource source);
    }

    /// <summary>
    /// 文档源定义
    /// </summary>
    public class DocumentSource
    {
        public DocumentSourceType Type { get; set; }
        public string FilePath { get; set; }
        public Stream Stream { get; set; }
        public string Directory { get; set; }

        public static DocumentSource FromFile(string filePath)
        {
            return new DocumentSource
            {
                Type = DocumentSourceType.File,
                FilePath = filePath
            };
        }

        public static DocumentSource FromStream(Stream stream)
        {
            return new DocumentSource
            {
                Type = DocumentSourceType.Stream,
                Stream = stream
            };
        }

        public static DocumentSource FromDirectory(string directory)
        {
            return new DocumentSource
            {
                Type = DocumentSourceType.Directory,
                Directory = directory
            };
        }
    }

    /// <summary>
    /// 文档源类型
    /// </summary>
    public enum DocumentSourceType
    {
        File,
        Stream,
        Directory
    }

    /// <summary>
    /// 文档加载选项
    /// </summary>
    public class LoadOptions
    {
        public bool EnableCaching { get; set; } = true;
        public int MaxCachePages { get; set; } = 10;
        public bool ValidateOnLoad { get; set; } = true;
        public bool PreloadFirstPage { get; set; } = true;
        public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public OfdVersion Version { get; set; }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Location { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Location { get; set; }
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }

    /// <summary>
    /// OFD版本
    /// </summary>
    public enum OfdVersion
    {
        V1_0,
        V1_1,
        V2_0,
        Unknown
    }

    /// <summary>
    /// 文档元数据
    /// </summary>
    public class DocumentMetadata
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Subject { get; set; }
        public string Creator { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime ModificationDate { get; set; }
        public OfdVersion Version { get; set; }
        public int PageCount { get; set; }
        public long FileSize { get; set; }
    }
}
