using System.Collections.Generic;

namespace OfdrwNet.Core
{
    /// <summary>
    /// 验证结果类
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; private set; } = true;

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; } = new();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="error">错误信息</param>
        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        /// <param name="warning">警告信息</param>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>
        /// 合并另一个验证结果
        /// </summary>
        /// <param name="other">另一个验证结果</param>
        public void Merge(ValidationResult other)
        {
            if (!other.IsValid)
            {
                IsValid = false;
            }

            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
        }

        /// <summary>
        /// 获取完整的验证信息
        /// </summary>
        /// <returns>验证信息字符串</returns>
        public string GetFullMessage()
        {
            var message = $"验证结果: {(IsValid ? "通过" : "失败")}\n";
            
            if (Errors.Count > 0)
            {
                message += "错误:\n";
                foreach (var error in Errors)
                {
                    message += $"  - {error}\n";
                }
            }
            
            if (Warnings.Count > 0)
            {
                message += "警告:\n";
                foreach (var warning in Warnings)
                {
                    message += $"  - {warning}\n";
                }
            }
            
            return message;
        }
    }
}