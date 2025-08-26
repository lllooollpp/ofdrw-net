using Microsoft.Extensions.Logging;

namespace OfdrwNet.WinFormsDemo;

/// <summary>
/// 程序入口点
/// </summary>
internal static class Program
{
    /// <summary>
    /// 应用程序的主入口点
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 启用应用程序的可视样式
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // 设置高DPI支持
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        
        // 设置全局异常处理
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        try
        {
            // 创建日志记录器
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole()
                       .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger("Program");
            logger.LogInformation("OFDRW.NET WinForms Demo 应用程序启动");
            
            // 运行主窗体
            Application.Run(new MainForm());
            
            logger.LogInformation("OFDRW.NET WinForms Demo 应用程序退出");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序启动失败: {ex.Message}", "严重错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    /// <summary>
    /// 处理应用程序线程异常
    /// </summary>
    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        LogException("应用程序线程异常", e.Exception);
        
        var result = MessageBox.Show(
            $"应用程序发生异常:\n{e.Exception.Message}\n\n是否继续运行？", 
            "应用程序错误", 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Error);
        
        if (result == DialogResult.No)
        {
            Application.Exit();
        }
    }
    
    /// <summary>
    /// 处理应用程序域未处理异常
    /// </summary>
    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogException("应用程序域未处理异常", ex);
            
            MessageBox.Show(
                $"应用程序发生严重错误:\n{ex.Message}\n\n应用程序将退出。", 
                "严重错误", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
        
        Environment.Exit(1);
    }
    
    /// <summary>
    /// 记录异常信息
    /// </summary>
    private static void LogException(string context, Exception exception)
    {
        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("Program");
            
            logger.LogError(exception, "{Context}: {Message}", context, exception.Message);
        }
        catch
        {
            // 忽略日志记录失败
        }
    }
}