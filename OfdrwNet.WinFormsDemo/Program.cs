using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using Serilog;
using Serilog.Events;

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
            // 创建日志目录（尝试查找解决方案根目录，否则使用可执行目录）
            var solutionRoot = GetSolutionRoot();
            var logDir = Path.Combine(solutionRoot ?? AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            // Serilog 自检日志（记录内部错误）
            try
            {
                Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(File.AppendText(Path.Combine(logDir, "serilog_selflog.txt"))));
            }
            catch
            {
                // 忽略 SelfLog 启用失败
            }

            // 配置 Serilog：同时输出到控制台和根目录下的 logs 文件夹（按天滚动）
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(logDir, "ofdrw_.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31, shared: true)
                .CreateLogger();

            // 注册 Trace -> Serilog 转发监听器（避免库中 Debug/Trace.WriteLine 丢失）
            var serilogListener = new SerilogTraceListener();
            if (!Trace.Listeners.OfType<SerilogTraceListener>().Any())
            {
                Trace.Listeners.Add(serilogListener);
                Trace.AutoFlush = true;
            }

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            var logger = loggerFactory.CreateLogger("Program");
            logger.LogDebug("OFDRW.NET WinForms Demo 应用程序启动");
            logger.LogInformation("日志配置完成，输出目录: {LogDirectory}", logDir);

            // 运行主窗体，传入 loggerFactory 以确保所有日志写入 Serilog
            Application.Run(new MainForm(loggerFactory));
            
            logger.LogInformation("OFDRW.NET WinForms Demo 应用程序退出");
        }
        catch (Exception ex)
        {
            // Serilog 可能尚未初始化，因此使用消息框保证用户可见
            MessageBox.Show($"应用程序启动失败: {ex.Message}", "严重错误", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // 确保 Serilog 刷新并关闭
            try { Log.CloseAndFlush(); } catch { }
        }
    }
    
    /// <summary>
    /// 获取解决方案根目录（尝试根据当前目录向上查找 .sln 文件）
    /// 如果找不到，则返回 null
    /// </summary>
    private static string? GetSolutionRoot()
    {
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var slnFiles = dir.GetFiles("*.sln");
                if (slnFiles.Length > 0) return dir.FullName;
                dir = dir.Parent;
            }
        }
        catch
        {
            // 忽略任何错误，返回 null
        }
        return null;
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
            // 使用 Serilog 记录异常，如果未初始化则会安全地忽略
            Log.Error(exception, "{Context}: {Message}", context, exception.Message);
        }
        catch
        {
            // 忽略日志记录失败
        }
    }
}