using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Sign;

/// <summary>
/// 签名提供者注册表。
/// </summary>
/// <remarks>
/// 动态管理 ISigner 实现,支持插件式签章适配。
///
/// 功能:
/// - 注册签名提供者(SM2/RSA/自定义)
/// - 按名称/算法类型解析提供者
/// - 默认提供者管理
/// - 提供者枚举和查询
///
/// 使用场景:
/// - CLI 签名命令动态选择签名算法
/// - 企业集成自定义签章服务
/// - 多算法签名策略切换
/// </remarks>
public sealed class SignerRegistry
{
    private readonly ILogger<SignerRegistry> _logger;
    private readonly Dictionary<string, ISigner> _providers;
    private string? _defaultProviderName;

    /// <summary>
    /// 初始化 SignerRegistry 实例。
    /// </summary>
    public SignerRegistry(ILogger<SignerRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = new Dictionary<string, ISigner>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 注册签名提供者。
    /// </summary>
    /// <param name="name">提供者名称(不区分大小写)</param>
    /// <param name="provider">签名提供者实例</param>
    /// <param name="setAsDefault">是否设为默认提供者</param>
    public void Register(string name, ISigner provider, bool setAsDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
        }

        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (_providers.ContainsKey(name))
        {
            _logger.LogWarning("Overwriting existing provider '{Name}'", name);
        }

        _providers[name] = provider;
        _logger.LogInformation("Registered signature provider '{Name}' (Type: {Type})", name, provider.GetType().Name);

        if (setAsDefault || _defaultProviderName == null)
        {
            _defaultProviderName = name;
            _logger.LogInformation("Set '{Name}' as default provider", name);
        }
    }

    /// <summary>
    /// 注销签名提供者。
    /// </summary>
    /// <param name="name">提供者名称</param>
    /// <returns>注销成功返回 true</returns>
    public bool Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var removed = _providers.Remove(name);

        if (removed)
        {
            _logger.LogInformation("Unregistered signature provider '{Name}'", name);

            // 如果注销的是默认提供者,清除默认设置
            if (string.Equals(_defaultProviderName, name, StringComparison.OrdinalIgnoreCase))
            {
                _defaultProviderName = _providers.Keys.FirstOrDefault();
                if (_defaultProviderName != null)
                {
                    _logger.LogInformation("Default provider changed to '{Name}'", _defaultProviderName);
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// 按名称解析签名提供者。
    /// </summary>
    /// <param name="name">提供者名称(可选,为 null 时返回默认提供者)</param>
    /// <returns>签名提供者实例</returns>
    /// <exception cref="InvalidOperationException">未找到提供者</exception>
    public ISigner Resolve(string? name = null)
    {
        var providerName = name ?? _defaultProviderName;

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("No signature provider specified and no default provider set");
        }

        if (!_providers.TryGetValue(providerName, out var provider))
        {
            throw new InvalidOperationException($"Signature provider '{providerName}' not found");
        }

        _logger.LogDebug("Resolved signature provider '{Name}'", providerName);
        return provider;
    }

    /// <summary>
    /// 尝试按名称解析签名提供者。
    /// </summary>
    /// <param name="name">提供者名称</param>
    /// <param name="provider">输出参数,签名提供者实例</param>
    /// <returns>找到返回 true</returns>
    public bool TryResolve(string name, out ISigner? provider)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            provider = null;
            return false;
        }

        return _providers.TryGetValue(name, out provider);
    }

    /// <summary>
    /// 按算法类型查找提供者。
    /// </summary>
    /// <param name="algorithm">算法名称(如 "SM2", "RSA")</param>
    /// <returns>支持该算法的第一个提供者,未找到则抛出异常</returns>
    public ISigner ResolveByAlgorithm(string algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            throw new ArgumentException("Algorithm cannot be null or empty", nameof(algorithm));
        }

        // 简化实现:按名称匹配(实际场景可能需要 ISignatureProvider 暴露 SupportedAlgorithms 属性)
        var provider = _providers.Values.FirstOrDefault(p =>
            p.GetType().Name.Contains(algorithm, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            throw new InvalidOperationException($"No provider found for algorithm '{algorithm}'");
        }

        _logger.LogDebug("Resolved provider for algorithm '{Algorithm}': {Provider}", algorithm, provider.GetType().Name);
        return provider;
    }

    /// <summary>
    /// 设置默认提供者。
    /// </summary>
    /// <param name="name">提供者名称</param>
    public void SetDefault(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Provider name cannot be null or empty", nameof(name));
        }

        if (!_providers.ContainsKey(name))
        {
            throw new InvalidOperationException($"Provider '{name}' is not registered");
        }

        _defaultProviderName = name;
        _logger.LogInformation("Default signature provider set to '{Name}'", name);
    }

    /// <summary>
    /// 获取默认提供者名称。
    /// </summary>
    public string? GetDefaultProviderName() => _defaultProviderName;

    /// <summary>
    /// 获取所有已注册提供者的名称。
    /// </summary>
    public IReadOnlyList<string> GetProviderNames()
    {
        return _providers.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 获取已注册提供者数量。
    /// </summary>
    public int Count => _providers.Count;

    /// <summary>
    /// 检查提供者是否已注册。
    /// </summary>
    public bool IsRegistered(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && _providers.ContainsKey(name);
    }

    /// <summary>
    /// 清除所有已注册提供者。
    /// </summary>
    public void Clear()
    {
        _logger.LogInformation("Clearing all registered providers (Count: {Count})", _providers.Count);
        _providers.Clear();
        _defaultProviderName = null;
    }
}
