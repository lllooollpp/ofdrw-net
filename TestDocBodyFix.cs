using System;
using OfdrwNet.Reader;

namespace TestDocBodyFix
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("测试DocBody修复...");
            
            try
            {
                // 测试读取OFD文件
                using (var reader = new OfdReader("simple_test.ofd"))
                {
                    Console.WriteLine($"成功打开OFD文件");
                    var pageCount = reader.GetNumberOfPages();
                    Console.WriteLine($"页面数量: {pageCount}");
                    var pageList = reader.GetPageList();
                    Console.WriteLine($"页面列表长度: {pageList.Count}");
                    Console.WriteLine("✅ DocBody修复成功！");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return;
            }
        }
    }
}
