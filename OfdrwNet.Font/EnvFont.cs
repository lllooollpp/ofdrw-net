using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace OfdrwNet.Font;

/// <summary>
/// 环境变量中的字体管理器
/// 对应 Java 版本的 org.ofdrw.font.EnvFont
/// 负责系统字体的加载、管理和字符串尺寸计算
/// </summary>
public static class EnvFont
{
    /// <summary>
    /// 是否初始化完成
    /// </summary>
    private static volatile bool _isInitialized = false;

    /// <summary>
    /// 字体缓存映射表
    /// </summary>
    private static readonly Dictionary<string, System.Drawing.Font> _fontMap = new();

    /// <summary>
    /// 字体族缓存映射表
    /// </summary>
    private static readonly Dictionary<string, FontFamily> _fontFamilyMap = new();

    /// <summary>
    /// 默认字体
    /// 宋体 或 Serif、若都不存在则选择字体文件中出现的第一个
    /// </summary>
    private static System.Drawing.Font? _defaultFont;

    /// <summary>
    /// Graphics 对象用于字符串尺寸测量
    /// </summary>
    private static Graphics? _graphics;

    /// <summary>
    /// 同步锁对象
    /// </summary>
    private static readonly object _lock = new object();

    /// <summary>
    /// 在当前环境中寻找指定名称的字体
    /// </summary>
    /// <param name="name">字体名</param>
    /// <returns>指定名称字体，若不存在则返回null</returns>
    public static System.Drawing.Font? GetFont(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        Initialize();
        
        string lowerName = name.ToLowerInvariant();
        return _fontMap.TryGetValue(lowerName, out var font) ? font : null;
    }

    /// <summary>
    /// 在当前环境中寻找指定名称的字体
    /// </summary>
    /// <param name="name">字体名</param>
    /// <param name="family">替换字体名</param>
    /// <returns>指定名称字体，若不存在则返回null</returns>
    public static System.Drawing.Font? GetFont(string? name, string? family)
    {
        System.Drawing.Font? result = null;
        
        if (!string.IsNullOrWhiteSpace(name))
        {
            result = GetFont(name);
        }
        
        if (result != null)
        {
            return result;
        }
        
        if (!string.IsNullOrWhiteSpace(family))
        {
            result = GetFont(family);
        }
        
        return result;
    }

    /// <summary>
    /// 设置自定义文字映射
    /// </summary>
    /// <param name="name">字体名</param>
    /// <param name="font">字体对象</param>
    public static void SetMapping(string name, System.Drawing.Font font)
    {
        if (string.IsNullOrWhiteSpace(name) || font == null)
        {
            return;
        }

        lock (_lock)
        {
            _fontMap[name.ToLowerInvariant()] = font;
        }
    }

    /// <summary>
    /// 从目录中加载字体，仅加载以 .otf 或 .ttf 结尾的字体文件
    /// </summary>
    /// <param name="dirPath">字体文件所在目录</param>
    public static void Load(string dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
        {
            return;
        }

        Initialize();

        var fontFiles = Directory.GetFiles(dirPath, "*.ttf", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(dirPath, "*.otf", SearchOption.AllDirectories));

        foreach (var fontFile in fontFiles)
        {
            try
            {
                LoadFontFromFile(fontFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载字体文件失败：{fontFile}，错误：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 分析字符串大小在指定字号下所占空间大小
    /// 若无法找到字体则使用默认字体计算
    /// </summary>
    /// <param name="name">字体名</param>
    /// <param name="family">替换字体名</param>
    /// <param name="text">待分析字符串</param>
    /// <param name="size">字体大小</param>
    /// <returns>字符所占区域大小</returns>
    public static SizeF GetStringBounds(string? name, string? family, string text, float size)
    {
        if (string.IsNullOrEmpty(text))
        {
            return SizeF.Empty;
        }

        var font = GetFont(name, family) ?? GetDefaultFont();
        if (font == null)
        {
            return SizeF.Empty;
        }

        // 创建指定大小的字体
        using var sizedFont = new System.Drawing.Font(font.FontFamily, size, font.Style);
        
        // 使用 Graphics 测量字符串
        var graphics = GetGraphics();
        return graphics.MeasureString(text, sizedFont);
    }

    /// <summary>
    /// 获取默认字体
    /// </summary>
    /// <returns>默认字体</returns>
    public static System.Drawing.Font? GetDefaultFont()
    {
        Initialize();
        return _defaultFont;
    }

    /// <summary>
    /// 设置默认字体
    /// </summary>
    /// <param name="defaultFont">默认字体</param>
    public static void SetDefaultFont(System.Drawing.Font defaultFont)
    {
        _defaultFont = defaultFont ?? throw new ArgumentNullException(nameof(defaultFont));
    }

    /// <summary>
    /// 设置默认字体
    /// </summary>
    /// <param name="fontPath">字体文件路径</param>
    public static void SetDefaultFont(string fontPath)
    {
        if (string.IsNullOrWhiteSpace(fontPath) || !File.Exists(fontPath))
        {
            throw new FileNotFoundException("字体文件不存在", fontPath);
        }

        try
        {
            var privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(fontPath);
            
            if (privateFontCollection.Families.Length > 0)
            {
                _defaultFont = new System.Drawing.Font(privateFontCollection.Families[0], 12);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("字体文件格式错误或无法加载", ex);
        }
    }

    /// <summary>
    /// 字体加载初始化，仅在首次执行时加载，防止由于并发读取字体造成的异常
    /// </summary>
    private static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_isInitialized)
            {
                return;
            }

            LoadSystemFonts();
            InitializeDefaultFont();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// 加载系统字体
    /// </summary>
    private static void LoadSystemFonts()
    {
        try
        {
            // 获取已安装的字体族
            var installedFontCollection = new InstalledFontCollection();
            var fontFamilies = installedFontCollection.Families;

            foreach (var fontFamily in fontFamilies)
            {
                try
                {
                    // 尝试创建常规样式的字体
                    FontStyle style = FontStyle.Regular;
                    if (!fontFamily.IsStyleAvailable(FontStyle.Regular))
                    {
                        // 如果常规样式不可用，尝试其他样式
                        if (fontFamily.IsStyleAvailable(FontStyle.Bold))
                            style = FontStyle.Bold;
                        else if (fontFamily.IsStyleAvailable(FontStyle.Italic))
                            style = FontStyle.Italic;
                        else if (fontFamily.IsStyleAvailable(FontStyle.Underline))
                            style = FontStyle.Underline;
                        else if (fontFamily.IsStyleAvailable(FontStyle.Strikeout))
                            style = FontStyle.Strikeout;
                        else
                            continue; // 如果没有任何可用样式，跳过这个字体族
                    }

                    var font = new System.Drawing.Font(fontFamily, 12, style);
                    var lowerName = fontFamily.Name.ToLowerInvariant();
                    
                    _fontMap[lowerName] = font;
                    _fontFamilyMap[lowerName] = fontFamily;
                }
                catch
                {
                    // 忽略无法创建的字体
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载系统字体时发生错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 初始化默认字体
    /// </summary>
    private static void InitializeDefaultFont()
    {
        // 按优先级查找默认字体
        string[] fontPriorities = { "宋体", "simsun", "microsoftyahei", "微软雅黑", "arial", "times new roman", "serif" };
        
        foreach (var fontName in fontPriorities)
        {
            if (_fontMap.TryGetValue(fontName, out var font))
            {
                _defaultFont = font;
                return;
            }
        }

        // 如果找不到优先字体，使用第一个可用字体
        if (_fontMap.Count > 0)
        {
            _defaultFont = _fontMap.Values.First();
        }
        else
        {
            // 最后的备用方案：使用系统默认字体
            try
            {
                _defaultFont = new System.Drawing.Font(FontFamily.GenericSerif, 12);
            }
            catch
            {
                _defaultFont = SystemFonts.DefaultFont;
            }
        }
    }

    /// <summary>
    /// 从文件加载字体
    /// </summary>
    /// <param name="fontFilePath">字体文件路径</param>
    private static void LoadFontFromFile(string fontFilePath)
    {
        try
        {
            var privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(fontFilePath);
            
            foreach (var fontFamily in privateFontCollection.Families)
            {
                try
                {
                    var font = new System.Drawing.Font(fontFamily, 12);
                    var lowerName = fontFamily.Name.ToLowerInvariant();
                    
                    lock (_lock)
                    {
                        _fontMap[lowerName] = font;
                        _fontFamilyMap[lowerName] = fontFamily;
                    }
                }
                catch
                {
                    // 忽略无法创建的字体
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"加载字体文件失败：{fontFilePath}", ex);
        }
    }

    /// <summary>
    /// 获取用于字符串测量的Graphics对象
    /// </summary>
    /// <returns>Graphics对象</returns>
    private static Graphics GetGraphics()
    {
        if (_graphics == null)
        {
            lock (_lock)
            {
                if (_graphics == null)
                {
                    // 创建一个1x1像素的位图用于字符串测量
                    var bitmap = new Bitmap(1, 1);
                    _graphics = Graphics.FromImage(bitmap);
                    _graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                }
            }
        }
        return _graphics;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public static void Dispose()
    {
        lock (_lock)
        {
            _graphics?.Dispose();
            _graphics = null;
            
            foreach (var font in _fontMap.Values)
            {
                try
                {
                    font.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
            }
            _fontMap.Clear();
            _fontFamilyMap.Clear();
            
            _isInitialized = false;
        }
    }
}