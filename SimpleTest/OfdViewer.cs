using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace SimpleTest
{
    /// <summary>
    /// 增强型OFD内容查看器
    /// 提供详细的OFD文档分析、文本提取、结构分析等功能
    /// </summary>
    public class OfdViewer
    {
        private static readonly Dictionary<string, string> FileTypeDescriptions = new()
        {
            { "ofd.xml", "📋 OFD根文档" },
            { "document.xml", "📄 文档定义" },
            { "documentres.xml", "🎨 文档资源" },
            { "publicres.xml", "🌐 公共资源" },
            { "page_0.xml", "📃 页面内容" },
            { "content.xml", "📝 页面内容" }
        };

        /// <summary>
        /// 交互式OFD文档浏览器
        /// </summary>
        public static void InteractiveBrowser()
        {
            var ofdFiles = Directory.GetFiles(".", "*.ofd").OrderBy(f => f).ToArray();
            
            if (ofdFiles.Length == 0)
            {
                Console.WriteLine("❌ 当前目录中没有找到OFD文件");
                Console.WriteLine("请先创建一些OFD文件，然后再使用查看器。");
                return;
            }

            while (true)
            {
                Console.Clear();
                Console.WriteLine("🔍 OFD 文档浏览器");
                Console.WriteLine("═" + new string('═', 50));
                Console.WriteLine();
                
                for (int i = 0; i < ofdFiles.Length; i++)
                {
                    var fileInfo = new FileInfo(ofdFiles[i]);
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(ofdFiles[i])} ({fileInfo.Length} bytes)");
                }
                
                Console.WriteLine();
                Console.WriteLine("0. 退出浏览器");
                Console.WriteLine("A. 分析所有文件");
                Console.WriteLine("C. 比较两个文件");
                Console.WriteLine("T. 提取所有文本");
                Console.WriteLine();
                Console.Write("请选择操作 (0-{0}/A/C/T): ", ofdFiles.Length);
                
                var input = Console.ReadLine()?.ToUpper();
                
                if (input == "0")
                    break;
                else if (input == "A")
                    AnalyzeAllFiles(ofdFiles);
                else if (input == "C")
                    CompareFiles(ofdFiles);
                else if (input == "T")
                    ExtractAllTexts(ofdFiles);
                else if (int.TryParse(input, out int choice) && choice >= 1 && choice <= ofdFiles.Length)
                    DetailedView(ofdFiles[choice - 1]);
                else
                    Console.WriteLine("❌ 无效选择，请重试...");
                
                if (input != "0")
                {
                    Console.WriteLine();
                    Console.WriteLine("按任意键继续...");
                    Console.ReadKey();
                }
            }
        }
        /// <summary>
        /// 查看OFD文档内容
        /// </summary>
        /// <param name="ofdPath">OFD文件路径</param>
        public static void ViewContent(string ofdPath)
        {
            if (!File.Exists(ofdPath))
            {
                Console.WriteLine($"❌ 文件不存在: {ofdPath}");
                return;
            }

            Console.WriteLine($"👁️ 查看 OFD 文档内容: {Path.GetFileName(ofdPath)}");
            Console.WriteLine("=" + new string('=', 60));

            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
                    {
                        Console.WriteLine();
                        Console.WriteLine($"📄 {entry.FullName}");
                        Console.WriteLine("-" + new string('-', 40));

                        if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            DisplayXmlContent(entry);
                        }
                        else
                        {
                            Console.WriteLine($"   二进制文件 ({entry.Length} 字节)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 读取文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示XML文件内容
        /// </summary>
        private static void DisplayXmlContent(ZipArchiveEntry entry)
        {
            try
            {
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    
                    // 格式化XML
                    try
                    {
                        var doc = XDocument.Parse(content);
                        var formatted = doc.ToString();
                        
                        // 显示格式化的XML（限制长度以避免输出过长）
                        var lines = formatted.Split('\n');
                        var maxLines = 30; // 最多显示30行
                        
                        for (int i = 0; i < Math.Min(lines.Length, maxLines); i++)
                        {
                            Console.WriteLine($"   {lines[i]}");
                        }
                        
                        if (lines.Length > maxLines)
                        {
                            Console.WriteLine($"   ... (还有 {lines.Length - maxLines} 行)");
                        }
                        
                        // 显示XML摘要信息
                        AnalyzeXmlContent(doc, entry.FullName);
                    }
                    catch (XmlException)
                    {
                        // 如果XML格式不正确，显示原始内容
                        Console.WriteLine("   XML格式错误，显示原始内容:");
                        Console.WriteLine($"   {content}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 读取XML内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析XML内容并显示摘要
        /// </summary>
        private static void AnalyzeXmlContent(XDocument doc, string fileName)
        {
            Console.WriteLine();
            Console.WriteLine("   📊 XML 分析摘要:");

            try
            {
                var root = doc.Root;
                if (root != null)
                {
                    Console.WriteLine($"      根元素: {root.Name.LocalName}");
                    
                    if (root.HasAttributes)
                    {
                        Console.WriteLine($"      根元素属性:");
                        foreach (var attr in root.Attributes())
                        {
                            Console.WriteLine($"        - {attr.Name}: {attr.Value}");
                        }
                    }
                    
                    var childElements = root.Elements().ToList();
                    if (childElements.Any())
                    {
                        Console.WriteLine($"      子元素 ({childElements.Count}):");
                        var groupedElements = childElements.GroupBy(e => e.Name.LocalName)
                                                          .OrderBy(g => g.Key);
                        
                        foreach (var group in groupedElements)
                        {
                            Console.WriteLine($"        - {group.Key}: {group.Count()} 个");
                        }
                    }

                    // 特定文件的详细分析
                    switch (Path.GetFileName(fileName).ToLowerInvariant())
                    {
                        case "ofd.xml":
                            AnalyzeOfdXml(doc);
                            break;
                        case "document.xml":
                            AnalyzeDocumentXml(doc);
                            break;
                        default:
                            if (fileName.Contains("Page_"))
                            {
                                AnalyzePageXml(doc);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ XML分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析OFD.xml文件
        /// </summary>
        private static void AnalyzeOfdXml(XDocument doc)
        {
            Console.WriteLine("      📋 OFD文档信息:");
            
            var docBody = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "DocBody");
            if (docBody != null)
            {
                var docInfo = docBody.Element(docBody.Name.Namespace + "DocInfo");
                if (docInfo != null)
                {
                    var title = docInfo.Element(docInfo.Name.Namespace + "Title")?.Value;
                    var author = docInfo.Element(docInfo.Name.Namespace + "Author")?.Value;
                    var creator = docInfo.Element(docInfo.Name.Namespace + "Creator")?.Value;
                    
                    if (!string.IsNullOrEmpty(title))
                        Console.WriteLine($"        标题: {title}");
                    if (!string.IsNullOrEmpty(author))
                        Console.WriteLine($"        作者: {author}");
                    if (!string.IsNullOrEmpty(creator))
                        Console.WriteLine($"        创建者: {creator}");
                }
                
                var docRoot = docBody.Element(docBody.Name.Namespace + "DocRoot")?.Value;
                if (!string.IsNullOrEmpty(docRoot))
                {
                    Console.WriteLine($"        文档根目录: {docRoot}");
                }
            }
        }

        /// <summary>
        /// 分析Document.xml文件
        /// </summary>
        private static void AnalyzeDocumentXml(XDocument doc)
        {
            Console.WriteLine("      📋 文档结构信息:");
            
            var commonData = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "CommonData");
            if (commonData != null)
            {
                var pageArea = commonData.Element(commonData.Name.Namespace + "PageArea");
                if (pageArea != null)
                {
                    var physicalBox = pageArea.Element(pageArea.Name.Namespace + "PhysicalBox")?.Value;
                    if (!string.IsNullOrEmpty(physicalBox))
                    {
                        Console.WriteLine($"        页面大小: {physicalBox}");
                    }
                }
            }
            
            var pages = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Pages");
            if (pages != null)
            {
                var pageElements = pages.Elements().Where(e => e.Name.LocalName == "Page").ToList();
                Console.WriteLine($"        页面数量: {pageElements.Count}");
                
                foreach (var page in pageElements.Take(5)) // 只显示前5页的信息
                {
                    var id = page.Attribute("ID")?.Value;
                    var baseLoc = page.Attribute("BaseLoc")?.Value;
                    Console.WriteLine($"        页面 {id}: {baseLoc}");
                }
                
                if (pageElements.Count > 5)
                {
                    Console.WriteLine($"        ... 还有 {pageElements.Count - 5} 页");
                }
            }
        }

        /// <summary>
        /// 分析页面XML文件
        /// </summary>
        private static void AnalyzePageXml(XDocument doc)
        {
            Console.WriteLine("      📋 页面内容信息:");
            
            var content = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Content");
            if (content != null)
            {
                var layers = content.Elements().Where(e => e.Name.LocalName == "Layer").ToList();
                Console.WriteLine($"        图层数量: {layers.Count}");
                
                foreach (var layer in layers)
                {
                    var layerId = layer.Attribute("ID")?.Value ?? "未知";
                    var textObjects = layer.Descendants().Where(e => e.Name.LocalName == "TextObject").ToList();
                    var pathObjects = layer.Descendants().Where(e => e.Name.LocalName == "PathObject").ToList();
                    var imageObjects = layer.Descendants().Where(e => e.Name.LocalName == "ImageObject").ToList();
                    
                    Console.WriteLine($"        图层 {layerId}:");
                    Console.WriteLine($"          文本对象: {textObjects.Count}");
                    Console.WriteLine($"          路径对象: {pathObjects.Count}");
                    Console.WriteLine($"          图像对象: {imageObjects.Count}");
                    
                    // 显示文本内容
                    foreach (var textObj in textObjects.Take(3)) // 只显示前3个文本对象
                    {
                        var textCode = textObj.Descendants()
                                             .FirstOrDefault(e => e.Name.LocalName == "TextCode")?.Value;
                        if (!string.IsNullOrEmpty(textCode))
                        {
                            var preview = textCode.Length > 20 ? textCode.Substring(0, 20) + "..." : textCode;
                            Console.WriteLine($"          文本内容: \"{preview}\"");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 快速查看现有的所有OFD文件
        /// </summary>
        public static void ViewAllOfdFiles()
        {
            var ofdFiles = Directory.GetFiles(".", "*.ofd");
            
            if (ofdFiles.Length == 0)
            {
                Console.WriteLine("❌ 当前目录中没有找到OFD文件");
                return;
            }
            
            Console.WriteLine($"📁 找到 {ofdFiles.Length} 个OFD文件，开始分析...");
            Console.WriteLine();
            
            foreach (var file in ofdFiles.OrderBy(f => f))
            {
                ViewContent(file);
                Console.WriteLine();
                Console.WriteLine("=" + new string('=', 80));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 详细查看单个OFD文档
        /// </summary>
        private static void DetailedView(string ofdPath)
        {
            Console.Clear();
            Console.WriteLine($"🔍 详细查看: {Path.GetFileName(ofdPath)}");
            Console.WriteLine("═" + new string('═', 60));
            
            // 显示文件基本信息
            var fileInfo = new FileInfo(ofdPath);
            Console.WriteLine($"📊 文件信息:");
            Console.WriteLine($"   大小: {fileInfo.Length} 字节");
            Console.WriteLine($"   创建时间: {fileInfo.CreationTime}");
            Console.WriteLine($"   修改时间: {fileInfo.LastWriteTime}");
            Console.WriteLine();

            // 显示文档结构
            Console.WriteLine("📁 文档结构:");
            ViewStructure(ofdPath);
            Console.WriteLine();

            // 提取并显示文本内容
            Console.WriteLine("📝 文本内容:");
            ExtractTexts(ofdPath);
            Console.WriteLine();

            // 显示详细的XML内容
            Console.WriteLine("📄 详细内容 (按Enter继续，输入'q'跳过):");
            if (Console.ReadLine()?.ToLower() != "q")
            {
                ViewContent(ofdPath);
            }
        }

        /// <summary>
        /// 分析所有OFD文件
        /// </summary>
        private static void AnalyzeAllFiles(string[] ofdFiles)
        {
            Console.Clear();
            Console.WriteLine("📊 批量分析 OFD 文档");
            Console.WriteLine("═" + new string('═', 50));
            Console.WriteLine();

            var analysis = new List<(string File, long Size, int TextObjects, string FirstText)>();

            foreach (var file in ofdFiles)
            {
                Console.WriteLine($"分析中: {Path.GetFileName(file)}...");
                
                var fileInfo = new FileInfo(file);
                var (textCount, firstText) = GetTextSummary(file);
                
                analysis.Add((Path.GetFileName(file), fileInfo.Length, textCount, firstText));
            }

            Console.WriteLine();
            Console.WriteLine("📋 分析结果汇总:");
            Console.WriteLine("-".PadRight(80, '-'));
            Console.WriteLine($"{"文件名",-25} {"大小",-10} {"文本对象",-8} {"首个文本预览",-30}");
            Console.WriteLine("-".PadRight(80, '-'));

            foreach (var (file, size, textObjects, firstText) in analysis)
            {
                var preview = string.IsNullOrEmpty(firstText) ? "(无文本)" : 
                             (firstText.Length > 25 ? firstText.Substring(0, 25) + "..." : firstText);
                Console.WriteLine($"{file,-25} {size,-10} {textObjects,-8} {preview,-30}");
            }

            Console.WriteLine("-".PadRight(80, '-'));
            Console.WriteLine($"总计: {analysis.Count} 个文件, {analysis.Sum(a => a.Size)} 字节");
        }

        /// <summary>
        /// 比较两个OFD文件
        /// </summary>
        private static void CompareFiles(string[] ofdFiles)
        {
            if (ofdFiles.Length < 2)
            {
                Console.WriteLine("❌ 至少需要2个OFD文件才能进行比较");
                return;
            }

            Console.Clear();
            Console.WriteLine("⚖️ 比较 OFD 文档");
            Console.WriteLine("═" + new string('═', 40));
            Console.WriteLine();

            for (int i = 0; i < ofdFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(ofdFiles[i])}");
            }

            Console.Write("选择第一个文件 (1-{0}): ", ofdFiles.Length);
            if (!int.TryParse(Console.ReadLine(), out int file1) || file1 < 1 || file1 > ofdFiles.Length)
            {
                Console.WriteLine("❌ 无效选择");
                return;
            }

            Console.Write("选择第二个文件 (1-{0}): ", ofdFiles.Length);
            if (!int.TryParse(Console.ReadLine(), out int file2) || file2 < 1 || file2 > ofdFiles.Length)
            {
                Console.WriteLine("❌ 无效选择");
                return;
            }

            CompareOfdFiles(ofdFiles[file1 - 1], ofdFiles[file2 - 1]);
        }

        /// <summary>
        /// 提取所有文件的文本内容
        /// </summary>
        private static void ExtractAllTexts(string[] ofdFiles)
        {
            Console.Clear();
            Console.WriteLine("📝 提取所有文本内容");
            Console.WriteLine("═" + new string('═', 40));
            Console.WriteLine();

            foreach (var file in ofdFiles)
            {
                Console.WriteLine($"📄 {Path.GetFileName(file)}:");
                Console.WriteLine("-" + new string('-', 40));
                ExtractTexts(file);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 查看OFD文档结构
        /// </summary>
        private static void ViewStructure(string ofdPath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    var entries = archive.Entries.OrderBy(e => e.FullName).ToList();
                    
                    foreach (var entry in entries)
                    {
                        var icon = GetFileIcon(entry.FullName);
                        var description = GetFileDescription(entry.FullName);
                        Console.WriteLine($"   {icon} {entry.FullName} - {description} ({entry.Length} bytes)");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 读取文档结构失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 提取文本内容
        /// </summary>
        private static void ExtractTexts(string ofdPath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    var pageEntries = archive.Entries
                        .Where(e => e.FullName.Contains("Page") && e.FullName.EndsWith(".xml"))
                        .OrderBy(e => e.FullName);

                    int textCount = 0;
                    foreach (var entry in pageEntries)
                    {
                        using (var stream = entry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var content = reader.ReadToEnd();
                            var doc = XDocument.Parse(content);
                            
                            var textObjects = doc.Descendants()
                                .Where(e => e.Name.LocalName == "TextObject");
                            
                            foreach (var textObj in textObjects)
                            {
                                var textCodes = textObj.Descendants()
                                    .Where(e => e.Name.LocalName == "TextCode");
                                
                                foreach (var textCode in textCodes)
                                {
                                    if (!string.IsNullOrEmpty(textCode.Value))
                                    {
                                        textCount++;
                                        Console.WriteLine($"   [{textCount:D2}] {textCode.Value}");
                                    }
                                }
                            }
                        }
                    }
                    
                    if (textCount == 0)
                    {
                        Console.WriteLine("   (没有找到文本内容)");
                    }
                    else
                    {
                        Console.WriteLine($"   ✅ 共找到 {textCount} 个文本对象");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 提取文本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文本摘要信息
        /// </summary>
        private static (int TextCount, string FirstText) GetTextSummary(string ofdPath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    var pageEntries = archive.Entries
                        .Where(e => e.FullName.Contains("Page") && e.FullName.EndsWith(".xml"))
                        .OrderBy(e => e.FullName);

                    int textCount = 0;
                    string firstText = "";

                    foreach (var entry in pageEntries)
                    {
                        using (var stream = entry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            var content = reader.ReadToEnd();
                            var doc = XDocument.Parse(content);
                            
                            var textCodes = doc.Descendants()
                                .Where(e => e.Name.LocalName == "TextCode")
                                .Select(e => e.Value)
                                .Where(v => !string.IsNullOrEmpty(v));
                            

                            foreach (var text in textCodes)
                            {
                                textCount++;
                                if (string.IsNullOrEmpty(firstText))
                                    firstText = text;
                            }
                        }
                    }
                    
                    return (textCount, firstText);
                }
            }
            catch
            {
                return (0, "");
            }
        }

        /// <summary>
        /// 比较两个OFD文件
        /// </summary>
        private static void CompareOfdFiles(string file1, string file2)
        {
            Console.WriteLine();
            Console.WriteLine($"⚖️ 比较文档:");
            Console.WriteLine($"   文件1: {Path.GetFileName(file1)}");
            Console.WriteLine($"   文件2: {Path.GetFileName(file2)}");
            Console.WriteLine();

            // 比较文件大小
            var info1 = new FileInfo(file1);
            var info2 = new FileInfo(file2);
            
            Console.WriteLine($"📊 基本信息比较:");
            Console.WriteLine($"   文件大小: {info1.Length} vs {info2.Length} 字节");
            
            // 比较文本内容
            var (count1, first1) = GetTextSummary(file1);
            var (count2, first2) = GetTextSummary(file2);
            
            Console.WriteLine($"📝 文本内容比较:");
            Console.WriteLine($"   文本对象数量: {count1} vs {count2}");
            Console.WriteLine($"   首个文本: \"{first1}\" vs \"{first2}\"");
            
            // 比较文档结构
            Console.WriteLine($"📁 文档结构比较:");
            CompareStructures(file1, file2);
        }

        /// <summary>
        /// 比较两个文档的结构
        /// </summary>
        private static void CompareStructures(string file1, string file2)
        {
            try
            {
                var entries1 = GetFileEntries(file1);
                var entries2 = GetFileEntries(file2);
                
                var allFiles = entries1.Keys.Union(entries2.Keys).OrderBy(f => f);
                
                foreach (var fileName in allFiles)
                {
                    var exists1 = entries1.ContainsKey(fileName);
                    var exists2 = entries2.ContainsKey(fileName);
                    
                    if (exists1 && exists2)
                    {
                        var size1 = entries1[fileName];
                        var size2 = entries2[fileName];
                        var status = size1 == size2 ? "✅ 相同" : $"⚠️ 不同 ({size1} vs {size2})";
                        Console.WriteLine($"   {fileName}: {status}");
                    }
                    else if (exists1)
                    {
                        Console.WriteLine($"   {fileName}: ❌ 仅存在于文件1");
                    }
                    else
                    {
                        Console.WriteLine($"   {fileName}: ❌ 仅存在于文件2");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 结构比较失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件条目字典
        /// </summary>
        private static Dictionary<string, long> GetFileEntries(string ofdPath)
        {
            var result = new Dictionary<string, long>();
            using (var archive = ZipFile.OpenRead(ofdPath))
            {
                foreach (var entry in archive.Entries)
                {
                    result[entry.FullName] = entry.Length;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取文件图标
        /// </summary>
        private static string GetFileIcon(string fileName)
        {
            if (fileName.EndsWith(".xml")) return "📄";
            if (fileName.EndsWith(".font") || fileName.Contains("Font")) return "🔤";
            if (fileName.EndsWith(".jpg") || fileName.EndsWith(".png")) return "🖼️";
            return "📁";
        }

        /// <summary>
        /// 获取文件描述
        /// </summary>
        private static string GetFileDescription(string fileName)
        {
            var baseName = Path.GetFileName(fileName).ToLowerInvariant();
            return FileTypeDescriptions.TryGetValue(baseName, out var desc) ? desc : "📄 其他文件";
        }
    }
}
