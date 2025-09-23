using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OfdrwNet.Reader.Diagnostics
{
    /// <summary>
    /// 统一异常处理器
    /// 提供结构化的异常处理和日志记录
    /// </summary>
    public static class ExceptionHandler
    {
        private static readonly object _lockObject = new object();
        private static readonly List<IExceptionLogger> _loggers = new List<IExceptionLogger>();

        /// <summary>
        /// 添加异常日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static void AddLogger(IExceptionLogger logger)
        {
            lock (_lockObject)
            {
                if (logger != null && !_loggers.Contains(logger))
                {
                    _loggers.Add(logger);
                }
            }
        }

        /// <summary>
        /// 移除异常日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static bool RemoveLogger(IExceptionLogger logger)
        {
            lock (_lockObject)
            {
                return _loggers.Remove(logger);
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="exception">异常</param>
        /// <param name="context">上下文信息</param>
        /// <param name="memberName">调用成员名</param>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="sourceLineNumber">源代码行号</param>
        public static void Handle(Exception exception, string? context = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var errorInfo = new ErrorInfo
            {
                Exception = exception,
                Context = context,
                MemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber,
                Timestamp = DateTime.Now,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            // 记录到所有日志记录器
            lock (_lockObject)
            {
                foreach (var logger in _loggers)
                {
                    try
                    {
                        logger.LogException(errorInfo);
                    }
                    catch
                    {
                        // 忽略日志记录错误
                    }
                }
            }

            // 根据异常类型决定是否重新抛出
            if (ShouldRethrow(exception))
            {
                throw exception;
            }
        }

        /// <summary>
        /// 安全执行操作
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="context">上下文信息</param>
        /// <param name="memberName">调用成员名</param>
        /// <returns>是否执行成功</returns>
        public static bool SafeExecute(Action action, string? context = null,
            [CallerMemberName] string memberName = "")
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Handle(ex, context, memberName);
                return false;
            }
        }

        /// <summary>
        /// 安全执行操作（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">默认返回值</param>
        /// <param name="context">上下文信息</param>
        /// <param name="memberName">调用成员名</param>
        /// <returns>执行结果或默认值</returns>
        public static T SafeExecute<T>(Func<T> func, T defaultValue = default!, string? context = null,
            [CallerMemberName] string memberName = "")
        {
            try
            {
                return func != null ? func() : defaultValue;
            }
            catch (Exception ex)
            {
                Handle(ex, context, memberName);
                return defaultValue;
            }
        }

        /// <summary>
        /// 创建渲染异常
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        /// <returns>渲染异常</returns>
        public static RenderException CreateRenderException(string objectId, string message, Exception? innerException = null)
        {
            return new RenderException(objectId, message, innerException);
        }

        /// <summary>
        /// 创建资源异常
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        /// <returns>资源异常</returns>
        public static ResourceException CreateResourceException(string resourceId, string message, Exception? innerException = null)
        {
            return new ResourceException(resourceId, message, innerException);
        }

        // 私有方法

        /// <summary>
        /// 判断是否应该重新抛出异常
        /// </summary>
        private static bool ShouldRethrow(Exception exception)
        {
            // 对于致命异常，应该重新抛出
            return exception is OutOfMemoryException ||
                   exception is StackOverflowException ||
                   exception is AccessViolationException ||
                   exception is ThreadAbortException;
        }
    }

    /// <summary>
    /// 异常日志记录器接口
    /// </summary>
    public interface IExceptionLogger
    {
        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="errorInfo">错误信息</param>
        void LogException(ErrorInfo errorInfo);
    }

    /// <summary>
    /// 错误信息
    /// </summary>
    public class ErrorInfo
    {
        /// <summary>异常对象</summary>
        public Exception? Exception { get; set; }

        /// <summary>上下文信息</summary>
        public string? Context { get; set; }

        /// <summary>调用成员名</summary>
        public string MemberName { get; set; } = "";

        /// <summary>源文件路径</summary>
        public string SourceFilePath { get; set; } = "";

        /// <summary>源代码行号</summary>
        public int SourceLineNumber { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>线程ID</summary>
        public int ThreadId { get; set; }

        /// <summary>错误级别</summary>
        public ErrorLevel Level { get; set; } = ErrorLevel.Error;

        /// <summary>
        /// 获取错误摘要
        /// </summary>
        /// <returns>错误摘要字符串</returns>
        public string GetSummary()
        {
            return $"[{Level}] {Exception?.Message ?? "Unknown error"} " +
                   $"in {MemberName} at {System.IO.Path.GetFileName(SourceFilePath)}:{SourceLineNumber}";
        }
    }

    /// <summary>
    /// 错误级别枚举
    /// </summary>
    public enum ErrorLevel
    {
        /// <summary>信息</summary>
        Information,
        /// <summary>警告</summary>
        Warning,
        /// <summary>错误</summary>
        Error,
        /// <summary>致命错误</summary>
        Fatal
    }

    /// <summary>
    /// 渲染异常
    /// </summary>
    public class RenderException : Exception
    {
        /// <summary>对象ID</summary>
        public string ObjectId { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public RenderException(string objectId, string message, Exception? innerException = null)
            : base($"渲染对象 {objectId} 时发生错误: {message}", innerException)
        {
            ObjectId = objectId;
        }
    }

    /// <summary>
    /// 资源异常
    /// </summary>
    public class ResourceException : Exception
    {
        /// <summary>资源ID</summary>
        public string ResourceId { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceId">资源ID</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ResourceException(string resourceId, string message, Exception? innerException = null)
            : base($"访问资源 {resourceId} 时发生错误: {message}", innerException)
        {
            ResourceId = resourceId;
        }
    }

    /// <summary>
    /// 控制台异常日志记录器
    /// </summary>
    public class ConsoleExceptionLogger : IExceptionLogger
    {
        /// <summary>
        /// 记录异常到控制台
        /// </summary>
        /// <param name="errorInfo">错误信息</param>
        public void LogException(ErrorInfo errorInfo)
        {
            Console.WriteLine($"[{errorInfo.Timestamp:yyyy-MM-dd HH:mm:ss}] {errorInfo.GetSummary()}");
            if (errorInfo.Context != null)
            {
                Console.WriteLine($"  Context: {errorInfo.Context}");
            }
            if (errorInfo.Exception != null)
            {
                Console.WriteLine($"  Stack Trace: {errorInfo.Exception.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 内存异常日志记录器
    /// </summary>
    public class MemoryExceptionLogger : IExceptionLogger
    {
        private readonly Queue<ErrorInfo> _errors = new Queue<ErrorInfo>();
        private readonly int _maxErrors;
        private readonly object _lock = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxErrors">最大错误记录数</param>
        public MemoryExceptionLogger(int maxErrors = 1000)
        {
            _maxErrors = maxErrors;
        }

        /// <summary>
        /// 记录异常到内存
        /// </summary>
        /// <param name="errorInfo">错误信息</param>
        public void LogException(ErrorInfo errorInfo)
        {
            lock (_lock)
            {
                _errors.Enqueue(errorInfo);

                while (_errors.Count > _maxErrors)
                {
                    _errors.Dequeue();
                }
            }
        }

        /// <summary>
        /// 获取所有错误记录
        /// </summary>
        /// <returns>错误记录列表</returns>
        public List<ErrorInfo> GetAllErrors()
        {
            lock (_lock)
            {
                return new List<ErrorInfo>(_errors);
            }
        }

        /// <summary>
        /// 清空错误记录
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _errors.Clear();
            }
        }
    }
}

namespace OfdrwNet.Reader.Threading
{
    /// <summary>
    /// 线程安全工具类
    /// </summary>
    public static class ThreadSafetyUtils
    {
        /// <summary>
        /// 创建线程安全的字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <returns>线程安全字典</returns>
        public static System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue> CreateSafeDictionary<TKey, TValue>()
            where TKey : notnull
        {
            return new System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// 创建读写锁
        /// </summary>
        /// <returns>读写锁</returns>
        public static ReaderWriterLockSlim CreateReaderWriterLock()
        {
            return new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        /// 安全执行读操作
        /// </summary>
        /// <param name="rwLock">读写锁</param>
        /// <param name="readAction">读操作</param>
        public static void SafeRead(ReaderWriterLockSlim rwLock, Action readAction)
        {
            rwLock.EnterReadLock();
            try
            {
                readAction();
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 安全执行写操作
        /// </summary>
        /// <param name="rwLock">读写锁</param>
        /// <param name="writeAction">写操作</param>
        public static void SafeWrite(ReaderWriterLockSlim rwLock, Action writeAction)
        {
            rwLock.EnterWriteLock();
            try
            {
                writeAction();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 安全执行读操作（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="rwLock">读写锁</param>
        /// <param name="readFunc">读函数</param>
        /// <returns>读取结果</returns>
        public static T SafeRead<T>(ReaderWriterLockSlim rwLock, Func<T> readFunc)
        {
            rwLock.EnterReadLock();
            try
            {
                return readFunc();
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 安全执行写操作（带返回值）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="rwLock">读写锁</param>
        /// <param name="writeFunc">写函数</param>
        /// <returns>写入结果</returns>
        public static T SafeWrite<T>(ReaderWriterLockSlim rwLock, Func<T> writeFunc)
        {
            rwLock.EnterWriteLock();
            try
            {
                return writeFunc();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// 线程安全的缓存类
    /// </summary>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    public class ThreadSafeCache<TKey, TValue> : IDisposable
        where TKey : notnull
        where TValue : class
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache;
        private readonly ReaderWriterLockSlim _rwLock;
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cleanupInterval">清理间隔（毫秒）</param>
        public ThreadSafeCache(int cleanupInterval = 60000)
        {
            _cache = new System.Collections.Concurrent.ConcurrentDictionary<TKey, CacheEntry<TValue>>();
            _rwLock = new ReaderWriterLockSlim();
            _cleanupTimer = new Timer(CleanupExpiredItems, null, cleanupInterval, cleanupInterval);
        }

        /// <summary>
        /// 获取或添加缓存项
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="valueFactory">值工厂函数</param>
        /// <param name="expiration">过期时间</param>
        /// <returns>缓存值</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan? expiration = null)
        {
            var entry = _cache.GetOrAdd(key, k => new CacheEntry<TValue>
            {
                Value = valueFactory(k),
                ExpirationTime = expiration.HasValue ? DateTime.Now.Add(expiration.Value) : DateTime.MaxValue,
                LastAccessTime = DateTime.Now
            });

            entry.LastAccessTime = DateTime.Now;
            return entry.Value;
        }

        /// <summary>
        /// 尝试获取缓存值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">输出值</param>
        /// <returns>是否获取成功</returns>
        public bool TryGetValue(TKey key, out TValue? value)
        {
            if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired())
            {
                entry.LastAccessTime = DateTime.Now;
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>是否移除成功</returns>
        public bool Remove(TKey key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void Clear()
        {
            ThreadSafetyUtils.SafeWrite(_rwLock, () => _cache.Clear());
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存统计</returns>
        public CacheStatistics GetStatistics()
        {
            return ThreadSafetyUtils.SafeRead(_rwLock, () =>
            {
                var totalItems = _cache.Count;
                var expiredItems = 0;

                foreach (var entry in _cache.Values)
                {
                    if (entry.IsExpired())
                        expiredItems++;
                }

                return new CacheStatistics
                {
                    TotalItems = totalItems,
                    ExpiredItems = expiredItems,
                    ActiveItems = totalItems - expiredItems
                };
            });
        }

        /// <summary>
        /// 清理过期项
        /// </summary>
        private void CleanupExpiredItems(object? state)
        {
            if (_disposed) return;

            ThreadSafetyUtils.SafeWrite(_rwLock, () =>
            {
                var keysToRemove = new List<TKey>();

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.IsExpired())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _cache.TryRemove(key, out _);
                }
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _rwLock?.Dispose();
                _cache?.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 缓存条目
    /// </summary>
    internal class CacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime ExpirationTime { get; set; }
        public DateTime LastAccessTime { get; set; }

        public bool IsExpired()
        {
            return DateTime.Now > ExpirationTime;
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>总项目数</summary>
        public int TotalItems { get; set; }

        /// <summary>过期项目数</summary>
        public int ExpiredItems { get; set; }

        /// <summary>活动项目数</summary>
        public int ActiveItems { get; set; }

        /// <summary>命中率</summary>
        public double HitRate { get; set; }
    }
}
