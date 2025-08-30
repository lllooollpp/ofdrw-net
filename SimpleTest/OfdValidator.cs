using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text;

namespace SimpleTest
{
    /// <summary>
    /// OFD文档验证器
    /// 用于检查生成的OFD文档结构是否符合规范
    /// </summary>
    public class OfdValidator
    {
        /// <summary>
        /// 验证OFD文档
        /// </summary>
        /// <param name="ofdPath">OFD文件路径</param>
        /// <returns>验证结果</returns>
        public static ValidationResult Validate(string ofdPath)
        {
            var result = new ValidationResult();
            
            try
            {
                Console.WriteLine($"🔍 开始验证 OFD 文档: {ofdPath}");
                
                // 1. 检查文件是否存在
                if (!File.Exists(ofdPath))
                {
                    result.AddError($"文件不存在: {ofdPath}");
                    return result;
                }
                
                var fileInfo = new FileInfo(ofdPath);
                result.FileSize = fileInfo.Length;
                Console.WriteLine($"   📁 文件大小: {result.FileSize} 字节");
                
                // 2. 检查是否为有效的ZIP文件
                result.IsValidZip = CheckZipStructure(ofdPath, result);
                
                // 3. 检查OFD文档结构
                if (result.IsValidZip)
                {
                    result.IsValidOfdStructure = CheckOfdStructure(ofdPath, result);
                }
                
                // 4. 生成验证报告
                GenerateReport(result);
                
            }
            catch (Exception ex)
            {
                result.AddError($"验证过程中发生异常: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 检查ZIP结构
        /// </summary>
        private static bool CheckZipStructure(string ofdPath, ValidationResult result)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    result.ZipEntryCount = archive.Entries.Count;
                    Console.WriteLine($"   📦 ZIP条目数量: {result.ZipEntryCount}");
                    
                    foreach (var entry in archive.Entries)
                    {
                        result.ZipEntries.Add(entry.FullName);
                        Console.WriteLine($"      - {entry.FullName} ({entry.Length} 字节)");
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                result.AddError($"ZIP结构检查失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查OFD文档结构
        /// </summary>
        private static bool CheckOfdStructure(string ofdPath, ValidationResult result)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    // 检查必需的文件
                    var requiredFiles = new[] { "OFD.xml", "Doc/Document.xml" };
                    
                    foreach (var requiredFile in requiredFiles)
                    {
                        var entry = archive.GetEntry(requiredFile);
                        if (entry == null)
                        {
                            result.AddError($"缺少必需文件: {requiredFile}");
                        }
                        else
                        {
                            result.AddInfo($"✅ 找到必需文件: {requiredFile}");
                            
                            // 验证XML格式
                            ValidateXmlFile(entry, result);
                        }
                    }
                    
                    // 检查页面文件
                    var pageCount = 0;
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("Doc/Pages/Page_") && entry.FullName.EndsWith(".xml"))
                        {
                            pageCount++;
                            ValidateXmlFile(entry, result);
                        }
                    }
                    
                    result.PageCount = pageCount;
                    result.AddInfo($"📄 页面数量: {pageCount}");
                    
                    return result.Errors.Count == 0;
                }
            }
            catch (Exception ex)
            {
                result.AddError($"OFD结构检查失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 验证XML文件格式
        /// </summary>
        private static void ValidateXmlFile(ZipArchiveEntry entry, ValidationResult result)
        {
            try
            {
                using (var stream = entry.Open())
                using (var reader = XmlReader.Create(stream))
                {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    result.AddInfo($"✅ {entry.FullName} XML格式正确");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"{entry.FullName} XML格式错误: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 生成验证报告
        /// </summary>
        private static void GenerateReport(ValidationResult result)
        {
            Console.WriteLine();
            Console.WriteLine("📋 验证报告");
            Console.WriteLine("=" + new string('=', 40));
            
            if (result.Errors.Count == 0)
            {
                Console.WriteLine("✅ 验证通过！OFD文档结构正确。");
            }
            else
            {
                Console.WriteLine("❌ 验证失败！发现以下错误：");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   • {error}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("📊 详细信息:");
            Console.WriteLine($"   文件大小: {result.FileSize} 字节");
            Console.WriteLine($"   ZIP有效: {(result.IsValidZip ? "是" : "否")}");
            Console.WriteLine($"   OFD结构有效: {(result.IsValidOfdStructure ? "是" : "否")}");
            Console.WriteLine($"   ZIP条目数: {result.ZipEntryCount}");
            Console.WriteLine($"   页面数量: {result.PageCount}");
            
            if (result.Infos.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("ℹ️ 附加信息:");
                foreach (var info in result.Infos)
                {
                    Console.WriteLine($"   {info}");
                }
            }
        }
    }
    
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public long FileSize { get; set; }
        public bool IsValidZip { get; set; }
        public bool IsValidOfdStructure { get; set; }
        public int ZipEntryCount { get; set; }
        public int PageCount { get; set; }
        public List<string> ZipEntries { get; } = new();
        public List<string> Errors { get; } = new();
        public List<string> Infos { get; } = new();
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddInfo(string info)
        {
            Infos.Add(info);
        }
        
        public bool IsValid => Errors.Count == 0 && IsValidZip && IsValidOfdStructure;
    }
}
