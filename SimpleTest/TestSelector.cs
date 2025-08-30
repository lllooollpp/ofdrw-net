using System;

namespace SimpleTest
{
    /// <summary>
    /// 测试程序选择器
    /// </summary>
    class TestSelector
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("🔧 OFD 测试套件");
            Console.WriteLine("===============");
            Console.WriteLine();
            Console.WriteLine("请选择要运行的测试:");
            Console.WriteLine("1. 基础OFD生成测试 (BasicProgram.cs)");
            Console.WriteLine("2. 完整验证测试 (ValidationProgram.cs)");
            Console.WriteLine("3. 仅验证现有文件");
            Console.WriteLine("4. 🔍 交互式OFD浏览器 (增强版)");
            Console.WriteLine();
            Console.Write("请输入选择 (1-4): ");
            
            var choice = Console.ReadLine();
            Console.WriteLine();
            
            switch (choice)
            {
                case "1":
                    Console.WriteLine("🚀 运行基础OFD生成测试...");
                    await BasicProgram.Run(args);
                    break;
                    
                case "2":
                    Console.WriteLine("🚀 运行完整验证测试...");
                    await ValidationProgram.Run(args);
                    break;
                    
                case "3":
                    Console.WriteLine("🚀 验证现有OFD文件...");
                    ValidateExistingFiles();
                    break;
                    
                case "4":
                    Console.WriteLine("🔍 启动交互式OFD浏览器...");
                    OfdViewer.InteractiveBrowser();
                    break;
                    
                default:
                    Console.WriteLine("❌ 无效选择，运行默认测试...");
                    await ValidationProgram.Run(args);
                    break;
            }
        }
        
        /// <summary>
        /// 验证现有的OFD文件
        /// </summary>
        private static void ValidateExistingFiles()
        {
            var ofdFiles = System.IO.Directory.GetFiles(".", "*.ofd");
            
            if (ofdFiles.Length == 0)
            {
                Console.WriteLine("❌ 当前目录中没有找到OFD文件");
                return;
            }
            
            Console.WriteLine($"📁 找到 {ofdFiles.Length} 个OFD文件:");
            
            for (int i = 0; i < ofdFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {System.IO.Path.GetFileName(ofdFiles[i])}");
            }
            
            foreach (var file in ofdFiles)
            {
                Console.WriteLine();
                Console.WriteLine($"验证文件: {System.IO.Path.GetFileName(file)}");
                Console.WriteLine("-" + new string('-', 40));
                OfdValidator.Validate(file);
            }
        }
    }
}
