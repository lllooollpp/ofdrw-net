//namespace OfdrwNet.WinFormsDemo.Logging
//{
//    using System;
//    using System.IO;
//    using Microsoft.Extensions.Logging;

//    internal class FileLoggerProvider : ILoggerProvider
//    {
//        private readonly StreamWriter _writer;
//        private readonly object _lock = new();
//        private readonly LogLevel _minLevel;

//        public FileLoggerProvider(string logDirectory, LogLevel minLevel = LogLevel.Debug)
//        {
//            Directory.CreateDirectory(logDirectory);
//            var filePath = Path.Combine(logDirectory, $"ofdrw_{DateTime.Now:yyyyMMdd}.log");
//            _writer = new StreamWriter(new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
//            _minLevel = minLevel;
//        }

//        public ILogger CreateLogger(string categoryName) => new FileLogger(_writer, _lock, _minLevel, categoryName);

//        public void Dispose()
//        {
//            _writer?.Dispose();
//        }
//    }

//    // FileLogger.cs 已移除。
//    // 已改用 Serilog 进行日志记录（参见 Program.cs）。
//    // 如果需要从项目中物理删除此文件，请在文件资源管理器或 git 中删除它。
//}
