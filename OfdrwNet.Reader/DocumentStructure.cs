using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 文档结构对象
    /// 描述OFD文档的XML层次结构和解析状态
    /// </summary>
    public class DocumentStructure
    {
        /// <summary>
        /// OFD.xml根文档
        /// </summary>
        public XDocument? OfdXml { get; set; }

        /// <summary>
        /// Document.xml文档
        /// </summary>
        public XDocument? DocumentXml { get; set; }

        /// <summary>
        /// 页面XML文档集合，键为页面编号
        /// </summary>
        public Dictionary<int, XDocument> PageXmls { get; set; } = new Dictionary<int, XDocument>();

        /// <summary>
        /// 文档列表
        /// </summary>
        public List<string> DocumentList { get; set; } = new List<string>();

        /// <summary>
        /// 默认文档路径
        /// </summary>
        public string DefaultDocument { get; set; } = string.Empty;

        /// <summary>
        /// 资源映射关系，键为资源ID，值为资源路径
        /// </summary>
        public Dictionary<string, string> ResourceMappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 解析结果
        /// </summary>
        public ParseResult? ParseResult { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// OFD版本
        /// </summary>
        public OfdVersion Version { get; set; } = OfdVersion.Unknown;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DocumentStructure()
        {
        }

        /// <summary>
        /// 验证文档结构完整性
        /// </summary>
        /// <returns>验证是否通过</returns>
        public bool ValidateStructure()
        {
            ValidationErrors.Clear();

            // 检查OFD.xml
            if (OfdXml == null)
            {
                ValidationErrors.Add(new ValidationError
                {
                    Code = "STRUCT001",
                    Message = "缺少OFD.xml根文档",
                    Severity = ValidationSeverity.Error
                });
                return false;
            }

            // 检查Document.xml
            if (DocumentXml == null)
            {
                ValidationErrors.Add(new ValidationError
                {
                    Code = "STRUCT002",
                    Message = "缺少Document.xml文档",
                    Severity = ValidationSeverity.Error
                });
                return false;
            }

            // 检查页面XML
            if (PageXmls.Count == 0)
            {
                ValidationErrors.Add(new ValidationError
                {
                    Code = "STRUCT003",
                    Message = "文档必须包含至少一个页面",
                    Severity = ValidationSeverity.Error
                });
                return false;
            }

            // 检查页面编号连续性
            var pageNumbers = PageXmls.Keys.OrderBy(k => k).ToList();
            for (int i = 0; i < pageNumbers.Count; i++)
            {
                if (pageNumbers[i] != i + 1)
                {
                    ValidationErrors.Add(new ValidationError
                    {
                        Code = "STRUCT004",
                        Message = $"页面编号不连续，期望第{i + 1}页，实际第{pageNumbers[i]}页",
                        Severity = ValidationSeverity.Warning
                    });
                }
            }

            return ValidationErrors.All(e => e.Severity != ValidationSeverity.Error);
        }

        /// <summary>
        /// 获取验证错误列表
        /// </summary>
        /// <returns>验证错误列表</returns>
        public List<ValidationError> GetValidationErrors()
        {
            return new List<ValidationError>(ValidationErrors);
        }

        /// <summary>
        /// 添加页面XML
        /// </summary>
        /// <param name="pageNumber">页面编号</param>
        /// <param name="pageXml">页面XML文档</param>
        public void AddPageXml(int pageNumber, XDocument pageXml)
        {
            PageXmls[pageNumber] = pageXml;
        }

        /// <summary>
        /// 获取页面XML
        /// </summary>
        /// <param name="pageNumber">页面编号</param>
        /// <returns>页面XML文档</returns>
        public XDocument? GetPageXml(int pageNumber)
        {
            return PageXmls.TryGetValue(pageNumber, out var pageXml) ? pageXml : null;
        }

        /// <summary>
        /// 添加资源映射
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="resourcePath">资源路径</param>
        public void AddResourceMapping(string resourceId, string resourcePath)
        {
            ResourceMappings[resourceId] = resourcePath;
        }

        /// <summary>
        /// 获取资源路径
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <returns>资源路径</returns>
        public string? GetResourcePath(string resourceId)
        {
            return ResourceMappings.TryGetValue(resourceId, out var resourcePath) ? resourcePath : null;
        }

        /// <summary>
        /// 获取结构摘要信息
        /// </summary>
        /// <returns>结构摘要</returns>
        public string GetSummary()
        {
            return $"OFD结构: 版本={Version}, 页面数={PageXmls.Count}, " +
                   $"文档数={DocumentList.Count}, 资源数={ResourceMappings.Count}, " +
                   $"错误数={ValidationErrors.Count(e => e.Severity == ValidationSeverity.Error)}";
        }
    }

    /// <summary>
    /// 解析结果
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// 解析是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 解析耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 解析的元素数量
        /// </summary>
        public int ElementCount { get; set; }

        /// <summary>
        /// 解析错误列表
        /// </summary>
        public List<ParseError> Errors { get; set; } = new List<ParseError>();

        /// <summary>
        /// 解析警告列表
        /// </summary>
        public List<ParseWarning> Warnings { get; set; } = new List<ParseWarning>();

        /// <summary>
        /// 解析的文件列表
        /// </summary>
        public List<string> ParsedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 获取摘要信息
        /// </summary>
        /// <returns>摘要字符串</returns>
        public string GetSummary()
        {
            return $"解析结果: {(Success ? "成功" : "失败")}, " +
                   $"耗时={Duration.TotalMilliseconds:F1}ms, " +
                   $"元素数={ElementCount}, " +
                   $"错误数={Errors.Count}, " +
                   $"警告数={Warnings.Count}";
        }
    }

    /// <summary>
    /// 解析错误
    /// </summary>
    public class ParseError
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误位置（文件路径或XML路径）
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 列号
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// 解析警告
    /// </summary>
    public class ParseWarning
    {
        /// <summary>
        /// 警告代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 警告位置
        /// </summary>
        public string Location { get; set; } = string.Empty;
    }
}
