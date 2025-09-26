using System;
using System.Collections.Generic;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// OFD文档对象
    /// 表示完整的OFD文档，管理文档级别的元数据和资源
    /// </summary>
    public class OfdDocument : IDisposable
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文档元数据
        /// </summary>
        public DocumentMetadata? Metadata { get; set; }

        /// <summary>
        /// 文档信息 (兼容测试代码)
        /// </summary>
        public DocumentInfo? DocumentInfo { get; set; }

        /// <summary>
        /// 页面信息列表
        /// </summary>
        public List<PageInfo> Pages { get; set; } = new List<PageInfo>();

        /// <summary>
        /// 资源管理器
        /// </summary>
        public IResourceManager? Resources { get; set; }

        /// <summary>
        /// 文档结构
        /// </summary>
        public DocumentStructure? Structure { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public DocumentState State { get; set; } = DocumentState.NotLoaded;

        /// <summary>
        /// 加载时间
        /// </summary>
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// 内存使用量(字节)
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => Pages.Count;

        /// <summary>
        /// 页面尺寸（如果所有页面尺寸相同）
        /// </summary>
        public System.Drawing.Size? PageSize
        {
            get
            {
                if (Pages.Count == 0) return null;
                var firstPage = Pages[0];
                return new System.Drawing.Size((int)firstPage.Size.Width, (int)firstPage.Size.Height);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public OfdDocument()
        {
            LoadedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public OfdDocument(string filePath) : this()
        {
            FilePath = filePath;
        }

        /// <summary>
        /// 获取指定页面信息
        /// </summary>
        /// <param name="pageIndex">页面索引(0-based)</param>
        /// <returns>页面信息</returns>
        public PageInfo? GetPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count)
                return null;
            return Pages[pageIndex];
        }

        /// <summary>
        /// 获取指定页面信息
        /// </summary>
        /// <param name="pageNumber">页码(1-based)</param>
        /// <returns>页面信息</returns>
        public PageInfo? GetPageByNumber(int pageNumber)
        {
            return GetPage(pageNumber - 1);
        }

        /// <summary>
        /// 添加页面
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        public void AddPage(PageInfo pageInfo)
        {
            if (pageInfo != null)
            {
                Pages.Add(pageInfo);
            }
        }

        /// <summary>
        /// 移除页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count)
                return false;

            Pages.RemoveAt(pageIndex);
            return true;
        }

        /// <summary>
        /// 清空所有页面
        /// </summary>
        public void ClearPages()
        {
            Pages.Clear();
        }

        /// <summary>
        /// 更新内存使用量统计
        /// </summary>
        public void UpdateMemoryUsage()
        {
            long totalMemory = 0;

            // 计算页面占用内存
            foreach (var page in Pages)
            {
                // TODO: 在T027中扩展PageInfo后启用
                // if (page.Cache?.MemoryUsage > 0)
                // {
                //     totalMemory += page.Cache.MemoryUsage;
                // }
            }

            // 添加资源占用内存
            if (Resources != null)
            {
                var usageReport = Resources.GetUsageReportAsync().Result;
                totalMemory += usageReport.TotalMemoryUsed;
            }

            MemoryUsage = totalMemory;
        }

        /// <summary>
        /// 验证文档完整性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            // 检查基本属性
            if (string.IsNullOrEmpty(FilePath))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "DOC001",
                    Message = "文档文件路径不能为空",
                    Severity = ValidationSeverity.Error
                });
                result.IsValid = false;
            }

            // 检查页面
            if (Pages.Count == 0)
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "DOC002",
                    Message = "文档必须包含至少一个页面",
                    Severity = ValidationSeverity.Error
                });
                result.IsValid = false;
            }

            // 检查页面索引连续性
            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i];
                if (page.Index != i)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "DOC003",
                        Message = $"页面 {i} 的索引值不匹配: 期望 {i}, 实际 {page.Index}"
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 获取文档摘要信息
        /// </summary>
        /// <returns>摘要字符串</returns>
        public string GetSummary()
        {
            return $"OFD文档: {System.IO.Path.GetFileName(FilePath)}, " +
                   $"页数: {TotalPages}, " +
                   $"状态: {State}, " +
                   $"内存: {MemoryUsage / 1024.0:F1}KB, " +
                   $"加载时间: {LoadedAt:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && State != DocumentState.Disposed)
            {
                // 清理页面资源
                foreach (var page in Pages)
                {
                    // TODO: 在T027中扩展PageInfo后启用
                    // page.Cache?.RenderedBitmap?.Dispose();
                    // page.Cache?.ThumbnailBitmap?.Dispose();
                }

                Pages.Clear();

                // 清理资源管理器
                if (Resources is IDisposable disposableResources)
                {
                    disposableResources.Dispose();
                }

                State = DocumentState.Disposed;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~OfdDocument()
        {
            Dispose(false);
        }
    }
}
