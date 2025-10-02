using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace OfdrwNet.Converter.Interaction;

/// <summary>
/// 动作映射器。
/// </summary>
/// <remarks>
/// 将 PDF 动作（GoTo/URI/JavaScript 等）映射到 OFD 动作。
/// FR-18: 链接和动作转换
/// </remarks>
public sealed class ActionMapper
{
    private readonly ILogger<ActionMapper> _logger;
    private readonly BookmarkConverter _bookmarkConverter;

    /// <summary>
    /// 初始化 ActionMapper 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="bookmarkConverter">书签转换器（用于解析目标）</param>
    public ActionMapper(ILogger<ActionMapper> logger, BookmarkConverter bookmarkConverter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bookmarkConverter = bookmarkConverter ?? throw new ArgumentNullException(nameof(bookmarkConverter));
    }

    /// <summary>
    /// 映射 PDF 动作到 OFD 动作。
    /// </summary>
    /// <param name="pdfAction">PDF 动作对象</param>
    /// <returns>OFD 动作信息</returns>
    public ActionInfo? MapAction(object pdfAction)
    {
        if (pdfAction == null)
        {
            return null;
        }

        try
        {
            var actionType = GetActionType(pdfAction);
            if (string.IsNullOrWhiteSpace(actionType))
            {
                _logger.LogWarning("PDF action has no type");
                return null;
            }

            _logger.LogDebug("Mapping PDF action: {Type}", actionType);

            return actionType.ToLowerInvariant() switch
            {
                "goto" or "/goto" => MapGoToAction(pdfAction),
                "uri" or "/uri" => MapUriAction(pdfAction),
                "javascript" or "/javascript" => MapJavaScriptAction(pdfAction),
                "named" or "/named" => MapNamedAction(pdfAction),
                "gotoremote" or "/gotoremote" or "/gotor" => MapGoToRemoteAction(pdfAction),
                _ => MapUnsupportedAction(actionType, pdfAction)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to map PDF action");
            return null;
        }
    }

    /// <summary>
    /// 获取动作类型。
    /// </summary>
    private string? GetActionType(object action)
    {
        try
        {
            var type = action.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(action, null);
                if (pdfObject != null)
                {
                    var pdfObjectType = pdfObject.GetType();
                    var getAsNameMethod = pdfObjectType.GetMethod("GetAsName");

                    if (getAsNameMethod != null)
                    {
                        var actionTypeName = getAsNameMethod.Invoke(pdfObject, new object[] { "S" });
                        return actionTypeName?.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 映射 GoTo 动作（跳转到文档内页面）。
    /// </summary>
    private ActionInfo MapGoToAction(object pdfAction)
    {
        var info = new ActionInfo
        {
            Type = ActionType.GoTo
        };

        try
        {
            var destination = GetDestination(pdfAction);
            if (destination != null)
            {
                info.Destination = ParseDestination(destination);
                _logger.LogDebug("Mapped GoTo action to page {Page}", info.Destination);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract GoTo destination");
        }

        return info;
    }

    /// <summary>
    /// 映射 URI 动作（打开网页链接）。
    /// </summary>
    private ActionInfo MapUriAction(object pdfAction)
    {
        var info = new ActionInfo
        {
            Type = ActionType.Uri
        };

        try
        {
            var uri = GetUri(pdfAction);
            if (!string.IsNullOrWhiteSpace(uri))
            {
                info.Uri = uri;
                _logger.LogDebug("Mapped URI action: {Uri}", uri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract URI");
        }

        return info;
    }

    /// <summary>
    /// 映射 JavaScript 动作。
    /// </summary>
    private ActionInfo MapJavaScriptAction(object pdfAction)
    {
        var info = new ActionInfo
        {
            Type = ActionType.JavaScript
        };

        _logger.LogWarning("JavaScript action detected but not supported in OFD");

        try
        {
            var script = GetJavaScript(pdfAction);
            if (!string.IsNullOrWhiteSpace(script))
            {
                info.Script = script;
                info.ScriptHash = ComputeHash(script);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract JavaScript");
        }

        return info;
    }

    /// <summary>
    /// 映射命名动作（NextPage/PrevPage/FirstPage/LastPage 等）。
    /// </summary>
    private ActionInfo MapNamedAction(object pdfAction)
    {
        var info = new ActionInfo
        {
            Type = ActionType.Named
        };

        try
        {
            var namedAction = GetNamedAction(pdfAction);
            if (!string.IsNullOrWhiteSpace(namedAction))
            {
                info.NamedAction = namedAction;
                _logger.LogDebug("Mapped named action: {Name}", namedAction);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract named action");
        }

        return info;
    }

    /// <summary>
    /// 映射远程跳转动作（打开其他 PDF 文档）。
    /// </summary>
    private ActionInfo MapGoToRemoteAction(object pdfAction)
    {
        var info = new ActionInfo
        {
            Type = ActionType.GoToRemote
        };

        _logger.LogWarning("GoToRemote action detected but limited support in OFD");

        try
        {
            var fileName = GetRemoteFileName(pdfAction);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                info.RemoteFile = fileName;
            }

            var destination = GetDestination(pdfAction);
            if (destination != null)
            {
                info.Destination = ParseDestination(destination);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract remote action info");
        }

        return info;
    }

    /// <summary>
    /// 映射不支持的动作类型。
    /// </summary>
    private ActionInfo MapUnsupportedAction(string actionType, object pdfAction)
    {
        _logger.LogWarning("Unsupported PDF action type: {Type}", actionType);

        return new ActionInfo
        {
            Type = ActionType.Unsupported,
            UnsupportedType = actionType
        };
    }

    /// <summary>
    /// 获取目标位置。
    /// </summary>
    private object? GetDestination(object action)
    {
        try
        {
            var type = action.GetType();
            var getDestinationMethod = type.GetMethod("GetDestination");

            if (getDestinationMethod != null)
            {
                return getDestinationMethod.Invoke(action, null);
            }

            // 尝试从 PdfObject 获取 /D 键
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");
            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(action, null);
                if (pdfObject != null)
                {
                    var pdfObjectType = pdfObject.GetType();
                    var getMethod = pdfObjectType.GetMethod("Get");

                    if (getMethod != null)
                    {
                        return getMethod.Invoke(pdfObject, new object[] { "D" });
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析目标为页码。
    /// </summary>
    private string? ParseDestination(object destination)
    {
        try
        {
            // 占位实现：尝试提取页码
            var destStr = destination.ToString();
            if (!string.IsNullOrWhiteSpace(destStr))
            {
                // 简化处理：直接返回字符串表示
                return destStr;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取 URI 字符串。
    /// </summary>
    private string? GetUri(object action)
    {
        try
        {
            var type = action.GetType();
            var getUriMethod = type.GetMethod("GetUri");

            if (getUriMethod != null)
            {
                var uri = getUriMethod.Invoke(action, null);
                return uri?.ToString();
            }

            // 尝试从 PdfObject 获取 /URI 键
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");
            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(action, null);
                if (pdfObject != null)
                {
                    var pdfObjectType = pdfObject.GetType();
                    var getAsStringMethod = pdfObjectType.GetMethod("GetAsString");

                    if (getAsStringMethod != null)
                    {
                        var uriString = getAsStringMethod.Invoke(pdfObject, new object[] { "URI" });
                        return uriString?.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取 JavaScript 脚本。
    /// </summary>
    private string? GetJavaScript(object action)
    {
        try
        {
            var type = action.GetType();
            var getJsMethod = type.GetMethod("GetJavaScript") ?? type.GetMethod("GetJS");

            if (getJsMethod != null)
            {
                var script = getJsMethod.Invoke(action, null);
                return script?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取命名动作名称。
    /// </summary>
    private string? GetNamedAction(object action)
    {
        try
        {
            var type = action.GetType();
            var getNameMethod = type.GetMethod("GetName");

            if (getNameMethod != null)
            {
                var name = getNameMethod.Invoke(action, null);
                return name?.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取远程文件名。
    /// </summary>
    private string? GetRemoteFileName(object action)
    {
        try
        {
            var type = action.GetType();
            var getPdfObjectMethod = type.GetMethod("GetPdfObject");

            if (getPdfObjectMethod != null)
            {
                var pdfObject = getPdfObjectMethod.Invoke(action, null);
                if (pdfObject != null)
                {
                    var pdfObjectType = pdfObject.GetType();
                    var getAsDictionaryMethod = pdfObjectType.GetMethod("GetAsDictionary");

                    if (getAsDictionaryMethod != null)
                    {
                        var fileSpec = getAsDictionaryMethod.Invoke(pdfObject, new object[] { "F" });
                        if (fileSpec != null)
                        {
                            var fileSpecType = fileSpec.GetType();
                            var getAsStringMethod = fileSpecType.GetMethod("GetAsString");

                            if (getAsStringMethod != null)
                            {
                                var fileName = getAsStringMethod.Invoke(fileSpec, new object[] { "F" });
                                return fileName?.ToString();
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 计算字符串哈希（SHA-256）。
    /// </summary>
    private string ComputeHash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// 动作类型枚举。
/// </summary>
public enum ActionType
{
    /// <summary>
    /// 跳转到文档内页面。
    /// </summary>
    GoTo,

    /// <summary>
    /// 打开 URI（网页链接）。
    /// </summary>
    Uri,

    /// <summary>
    /// 执行 JavaScript。
    /// </summary>
    JavaScript,

    /// <summary>
    /// 命名动作（NextPage/PrevPage 等）。
    /// </summary>
    Named,

    /// <summary>
    /// 跳转到其他文档。
    /// </summary>
    GoToRemote,

    /// <summary>
    /// 不支持的动作类型。
    /// </summary>
    Unsupported
}

/// <summary>
/// 动作信息。
/// </summary>
public sealed class ActionInfo
{
    /// <summary>
    /// 动作类型。
    /// </summary>
    public ActionType Type { get; set; }

    /// <summary>
    /// 目标位置（页码或标识符）。
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// URI 地址。
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// JavaScript 脚本。
    /// </summary>
    public string? Script { get; set; }

    /// <summary>
    /// 脚本哈希（SHA-256）。
    /// </summary>
    public string? ScriptHash { get; set; }

    /// <summary>
    /// 命名动作名称。
    /// </summary>
    public string? NamedAction { get; set; }

    /// <summary>
    /// 远程文件名。
    /// </summary>
    public string? RemoteFile { get; set; }

    /// <summary>
    /// 不支持的动作类型名称。
    /// </summary>
    public string? UnsupportedType { get; set; }
}
