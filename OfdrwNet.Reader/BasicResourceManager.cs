using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 基本资源管理器实现
    /// 基于现有ResourceLocator提供简单的资源管理功能
    /// </summary>
    public class BasicResourceManager : IResourceManager
    {
        private readonly ResourceLocator _resourceLocator;
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();
        private readonly DocumentStructure? _documentStructure;

        /// <summary>
        /// 资源加载完成事件
        /// </summary>
        public event EventHandler<ResourceLoadedEventArgs>? ResourceLoaded;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceLocator">资源定位器</param>
        /// <param name="documentStructure">文档结构（可选，用于资源映射）</param>
        public BasicResourceManager(ResourceLocator resourceLocator, DocumentStructure? documentStructure = null)
        {
            _resourceLocator = resourceLocator ?? throw new ArgumentNullException(nameof(resourceLocator));
            _documentStructure = documentStructure;
        }

        /// <summary>
        /// 获取字体资源
        /// </summary>
        /// <param name="fontId">字体ID</param>
        /// <returns>字体对象</returns>
        public async Task<Font> GetFontAsync(string fontId)
        {
            return await Task.Run(() =>
            {
                if (_cache.TryGetValue($"font:{fontId}", out var cached) && cached is Font font)
                {
                    return font;
                }

                // TODO: 实际从资源定位器加载字体
                var newFont = new Font("Arial", 12);
                _cache[$"font:{fontId}"] = newFont;

                ResourceLoaded?.Invoke(this, new ResourceLoadedEventArgs
                {
                    ResourceId = fontId,
                    ResourceType = ResourceType.Font,
                    Size = 1024, // 估算大小
                    LoadDuration = TimeSpan.FromMilliseconds(10),
                    FromCache = false
                });

                return newFont;
            });
        }

        /// <summary>
        /// 尝试从字体ID解析字体信息
        /// </summary>
        private bool TryParseFontFromId(string fontId, out string fontName, out float fontSize, out FontStyle fontStyle)
        {
            fontName = "SimSun";
            fontSize = 12f;
            fontStyle = FontStyle.Regular;

            if (string.IsNullOrEmpty(fontId))
                return false;

            try
            {
                // 尝试从常见的OFD字体映射
                fontName = fontId.ToLower() switch
                {
                    var id when id.Contains("simsun") || id.Contains("宋体") => "SimSun",
                    var id when id.Contains("simhei") || id.Contains("黑体") => "SimHei",
                    var id when id.Contains("kaiti") || id.Contains("楷体") => "KaiTi",
                    var id when id.Contains("fangsong") || id.Contains("仿宋") => "FangSong",
                    var id when id.Contains("arial") => "Arial",
                    var id when id.Contains("times") => "Times New Roman",
                    var id when id.Contains("courier") => "Courier New",
                    _ => "SimSun" // 默认中文字体
                };

                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <param name="imageId">图像ID</param>
        /// <returns>图像对象</returns>
        public async Task<Image> GetImageAsync(string imageId)
        {
            return await Task.Run(() =>
            {
                if (_cache.TryGetValue($"image:{imageId}", out var cached) && cached is Image image)
                {
                    return image;
                }

                try
                {
                    // 首先尝试通过DocumentStructure的ResourceMappings查找
                    if (_documentStructure != null)
                    {
                        var resourcePath = _documentStructure.GetResourcePath(imageId);
                        if (!string.IsNullOrEmpty(resourcePath))
                        {
                            try
                            {
                                // 保存当前状态，然后尝试访问资源
                                _resourceLocator.Save();

                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 开始加载图像资源 ID={imageId}，映射路径={resourcePath}");
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] ResourceLocator当前工作目录={_resourceLocator.Pwd()}");

                                // ========== 路径策略重写 ==========
                                // resourcePath 可能是："Doc/Image_4.png" 或 直接 "Image_4.png" 或 生成端记录的其他形式。
                                // 实际解压根目录：Temp/<guid>/ ，真实文件多位于 Doc/Res/ 或 Doc_x/Res/
                                // 目标：根据当前根目录扫描可用 Doc/ / Doc_x/ 及 Res 目录，构建最可能命中的候选顺序。

                                var containerRoot = _resourceLocator.GetContainer(".");
                                var rootSysPath = containerRoot.GetSysAbsPath(); // C:\Users\...\Temp\<guid>
                                var fileNameOnly = Path.GetFileName(resourcePath).Replace("\\", "");
                                var lowerFile = fileNameOnly.ToLowerInvariant();

                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 根目录: {rootSysPath}");
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 目标文件名: {fileNameOnly}");

                                // 统一扩展尝试（优先保持原扩展）
                                string[] extPriority;
                                var ext = Path.GetExtension(fileNameOnly);
                                if (!string.IsNullOrEmpty(ext))
                                {
                                    extPriority = new[] { ext.ToLowerInvariant(), ".png", ".jpg", ".jpeg" };
                                }
                                else
                                {
                                    extPriority = new[] { ".png", ".jpg", ".jpeg" };
                                }

                                // 构建文档目录集合: Doc/, Doc_x/ (扫描), 以及根（无前缀）
                                var docDirs = new List<string>();
                                if (Directory.Exists(Path.Combine(rootSysPath, "Doc"))) docDirs.Add("Doc");
                                try
                                {
                                    foreach (var d in Directory.GetDirectories(rootSysPath, "Doc_*", SearchOption.TopDirectoryOnly))
                                    {
                                        var name = Path.GetFileName(d);
                                        if (!string.IsNullOrEmpty(name) && !docDirs.Contains(name)) docDirs.Add(name);
                                    }
                                }
                                catch { /* ignore scan errors */ }
                                // 若没有任何 Doc 目录，仍保留一个空前缀用于直接 root 查找
                                if (docDirs.Count == 0) docDirs.Add(string.Empty);

                                // 判断 resourcePath 是否已经包含 Res/ 或 Doc_/Res/ 结构
                                bool pathSeemsInRes = resourcePath.Contains("/Res/") || resourcePath.Contains("\\Res\\", StringComparison.OrdinalIgnoreCase);
                                var baseNameNoExt = Path.GetFileNameWithoutExtension(fileNameOnly);

                                var candidateOrder = new List<string>();
                                var candidateSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                                void AddCandidate(string rel)
                                {
                                    if (string.IsNullOrWhiteSpace(rel)) return;
                                    rel = rel.Replace('\\', '/');
                                    if (candidateSeen.Add(rel)) candidateOrder.Add(rel);
                                }

                                // 1. 原始 resourcePath（若不是绝对路径，将作为相对尝试）
                                if (!Path.IsPathRooted(resourcePath)) AddCandidate(resourcePath);
                                else
                                {
                                    // 若是绝对路径且位于 root 下，转换为相对
                                    if (resourcePath.StartsWith(rootSysPath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var rel = resourcePath.Substring(rootSysPath.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                                        AddCandidate(rel);
                                    }
                                }

                                // 2. 针对每个 Doc 目录构建 Res 与直接层级
                                foreach (var doc in docDirs)
                                {
                                    var prefix = string.IsNullOrEmpty(doc) ? string.Empty : doc + "/";
                                    foreach (var eExt in extPriority)
                                    {
                                        // Doc/Res/Image_4.png
                                        AddCandidate(prefix + "Res/" + baseNameNoExt + eExt);
                                        // Doc/Image_4.png   (有些写入器可能直接放 Doc/ 下)
                                        AddCandidate(prefix + fileNameOnly);
                                    }
                                }

                                // 3. 根级 Res/ 或直接根
                                foreach (var eExt in extPriority)
                                {
                                    AddCandidate("Res/" + baseNameNoExt + eExt);
                                    AddCandidate(baseNameNoExt + eExt);
                                }

                                // 4. 如果 resourcePath 中本身含 Doc_0/Image_X.png 形式，直接映射到 Doc/Res 同名
                                if (resourcePath.StartsWith("Doc_", StringComparison.OrdinalIgnoreCase) && !pathSeemsInRes)
                                {
                                    foreach (var eExt in extPriority)
                                    {
                                        AddCandidate("Doc/Res/" + baseNameNoExt + eExt);
                                    }
                                }

                                var pathsToTry = candidateOrder.ToArray();
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 生成候选路径 {pathsToTry.Length} 个，示例前3：{string.Join(',', pathsToTry.Take(3))} ...");
                                // ========== 路径策略结束 ==========

                                string? actualFilePath = null;

                                foreach (var pathToTry in pathsToTry)
                                {
                                    try
                                    {
                                        System.Diagnostics.Trace.WriteLine($"[ResourceManager] 尝试路径：{pathToTry}");
                                        actualFilePath = _resourceLocator.GetFile(pathToTry);
                                        if (actualFilePath != null)
                                        {
                                            System.Diagnostics.Trace.WriteLine($"[ResourceManager] 找到文件：{actualFilePath}");
                                            break;
                                        }
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        System.Diagnostics.Trace.WriteLine($"[ResourceManager] 路径不存在：{pathToTry}");
                                        // 继续尝试下一个路径
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Trace.WriteLine($"[ResourceManager] 路径查找异常 {pathToTry}: {ex.Message}");
                                    }
                                }

                                if (!string.IsNullOrEmpty(actualFilePath) && File.Exists(actualFilePath))
                                {
                                    var imageData = File.ReadAllBytes(actualFilePath);
                                    using var stream = new System.IO.MemoryStream(imageData);
                                    var resourceImage = Image.FromStream(stream);

                                    _cache[$"image:{imageId}"] = resourceImage;
                                    System.Diagnostics.Trace.WriteLine($"[ResourceManager] 成功加载图像资源 ID={imageId}，实际路径={actualFilePath}，尺寸={resourceImage.Width}x{resourceImage.Height}");

                                    ResourceLoaded?.Invoke(this, new ResourceLoadedEventArgs
                                    {
                                        ResourceId = imageId,
                                        ResourceType = ResourceType.Image,
                                        Size = imageData.Length,
                                        LoadDuration = TimeSpan.FromMilliseconds(10),
                                        FromCache = false
                                    });

                                    return resourceImage;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 通过ResourceMappings加载图像失败 ID={imageId}，路径={resourcePath}，错误：{ex.Message}");
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 异常详情: {ex}");
                            }
                            finally
                            {
                                _resourceLocator.Restore();
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"[ResourceManager] 未ResourceMappings中找到图像资源 ID={imageId}");
                        }
                    }

                    // 回退到原有的文件系统路径查找逻辑
                    _resourceLocator.Save();

                    // 尝试多种可能的路径
                    // 追加 Doc_? 目录支持（常见结构 Doc_0/Res/Image_xxx.PNG）
                    var docPrefixes = new List<string>{""};
                    try
                    {
                        // 粗略扫描根目录下 Doc_* 目录名（不触发异常就好），失败忽略
                        var rootContainer = _resourceLocator.GetContainer(".");
                        var rootSys = rootContainer.GetSysAbsPath();
                        foreach (var dir in System.IO.Directory.GetDirectories(rootSys, "Doc_*", System.IO.SearchOption.TopDirectoryOnly))
                        {
                            var name = System.IO.Path.GetFileName(dir);
                            if (!string.IsNullOrEmpty(name) && !docPrefixes.Contains(name+"/")) docPrefixes.Add(name+"/");
                        }
                    }
                    catch { /* 忽略扫描失败 */ }

                    var possiblePathsList = new List<string>();
                    foreach (var prefix in docPrefixes)
                    {
                        // prefix could be "" or "Doc_0/"
                        possiblePathsList.Add(prefix + $"Res/Image_{imageId}.png");
                        possiblePathsList.Add(prefix + $"Res/Image_{imageId}.jpg");
                        possiblePathsList.Add(prefix + $"Res/Image_{imageId}.jpeg");
                        possiblePathsList.Add(prefix + $"Image_{imageId}.png");
                        possiblePathsList.Add(prefix + $"Image_{imageId}.jpg");
                        possiblePathsList.Add(prefix + $"Resources/Image_{imageId}.png");
                        possiblePathsList.Add(prefix + $"Resources/Image_{imageId}.jpg");
                    }
                    var possiblePaths = possiblePathsList.ToArray();

                    Image? newImage = null;
                    string? usedPath = null;

                    foreach (var path in possiblePaths)
                    {
                        try
                        {
                            // 简化：直接构建路径访问
                            var container = _resourceLocator.GetContainer(".");
                            var fullPath = Path.Combine(container.GetSysAbsPath(), path);

                            if (File.Exists(fullPath))
                            {
                                var imageData = File.ReadAllBytes(fullPath);
                                using var stream = new System.IO.MemoryStream(imageData);
                                newImage = Image.FromStream(stream);
                                usedPath = path;
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] ID={imageId} 在路径 {path} 找到文件，尺寸={newImage.Width}x{newImage.Height}");
                                break;
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 路径不存在: {path}");
                            }
                        }
                        catch { /* 继续尝试下一个路径 */ }
                    }

                    if (newImage == null)
                    {
                        // 追加策略：利用 VirtualContainer 深度搜索能力，通过 ResourceLocator.GetFile("Image_{id}.ext")
                        // 这将触发我们在 VirtualContainer 中添加的 [Packaging-Fallback-Deep] 逻辑，从而获得真实路径 Doc_0/Res/Image_xx.PNG
                        var fallbackFileNames = new string[]
                        {
                            $"Image_{imageId}.png",
                            $"Image_{imageId}.jpg",
                            $"Image_{imageId}.jpeg",
                            $"Image_{imageId}.PNG",
                            $"Image_{imageId}.JPG",
                            $"Image_{imageId}.JPEG"
                        };
                        foreach (var fn in fallbackFileNames)
                        {
                            try
                            {
                                _resourceLocator.Save();
                                string located = _resourceLocator.GetFile(fn); // 若成功将自动返回绝对路径
                                if (System.IO.File.Exists(located))
                                {
                                    var imageData = System.IO.File.ReadAllBytes(located);
                                    using var stream = new System.IO.MemoryStream(imageData);
                                    newImage = Image.FromStream(stream);
                                    usedPath = fn;
                                    System.Diagnostics.Trace.WriteLine($"[ResourceManager] ID={imageId} 通过文件名搜索找到 => {located}");
                                    break;
                                }
                            }
                            catch (System.IO.FileNotFoundException)
                            {
                                // 继续尝试下一个
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 解析失败 ID={imageId} fn={fn} err={ex.Message}");
                            }
                            finally
                            {
                                _resourceLocator.Restore();
                            }
                        }
                    }

                    // NEW FALLBACK: 深度递归扫描（一次性），解决非标准命名或层级差异导致的缺图
                    if (newImage == null)
                    {
                        try
                        {
                            _resourceLocator.Save();
                            var container = _resourceLocator.GetContainer(".");
                            var rootSys = container.GetSysAbsPath();
                            string[] allowedExt = new[]{".png",".jpg",".jpeg",".bmp"};
                            // 限制扫描目录：优先 Res / Doc*/Res，如果不存在再全局
                            var candidateRoots = new List<string>();
                            if (Directory.Exists(Path.Combine(rootSys, "Res"))) candidateRoots.Add(Path.Combine(rootSys, "Res"));
                            try
                            {
                                foreach (var d in Directory.GetDirectories(rootSys, "Doc*", SearchOption.TopDirectoryOnly))
                                {
                                    var resDir = Path.Combine(d, "Res");
                                    if (Directory.Exists(resDir)) candidateRoots.Add(resDir);
                                }
                            }
                            catch { }
                            if (candidateRoots.Count == 0) candidateRoots.Add(rootSys); // 兜底全扫描

                            bool MatchFile(string file)
                            {
                                var ext = Path.GetExtension(file).ToLowerInvariant();
                                if (!allowedExt.Contains(ext)) return false;
                                var nameNoExt = Path.GetFileNameWithoutExtension(file);
                                // 命中规则：完全等于 ID，或 等于 Image_ID，或 包含 _ID 片段
                                if (nameNoExt.Equals(imageId, StringComparison.OrdinalIgnoreCase)) return true;
                                if ($"Image_{imageId}".Equals(nameNoExt, StringComparison.OrdinalIgnoreCase)) return true;
                                if (nameNoExt.EndsWith("_"+imageId, StringComparison.OrdinalIgnoreCase)) return true;
                                if (nameNoExt.Contains("_"+imageId+"_", StringComparison.OrdinalIgnoreCase)) return true;
                                return false;
                            }

                            string? foundFile = null;
                            foreach (var root in candidateRoots)
                            {
                                try
                                {
                                    foreach (var f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                                    {
                                        if (MatchFile(f)) { foundFile = f; break; }
                                    }
                                }
                                catch { }
                                if (foundFile != null) break;
                            }

                            if (foundFile != null && File.Exists(foundFile))
                            {
                                var imageData = File.ReadAllBytes(foundFile);
                                using var stream = new MemoryStream(imageData);
                                newImage = Image.FromStream(stream);
                                usedPath = foundFile.Substring(rootSys.Length).TrimStart(Path.DirectorySeparatorChar, '/', '\\');
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 深度扫描匹配 ID={imageId} => {foundFile}");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"[ResourceManager] 深度扫描未命中 ID={imageId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine($"[ResourceManager] 深度扫描异常 ID={imageId} err={ex.Message}");
                        }
                        finally
                        {
                            _resourceLocator.Restore();
                        }
                    }

                    if (newImage == null)
                    {
                        // 创建一个占位图像，避免返回null导致崩溃
                        newImage = new Bitmap(50, 20);
                        using (var g = Graphics.FromImage(newImage))
                        {
                            g.Clear(Color.LightGray);
                            g.DrawString($"IMG{imageId}", new Font("Arial", 8), Brushes.Red, 2, 2);
                        }
                        System.Diagnostics.Trace.WriteLine($"[ResourceManager] 未找到图像资源 ID={imageId}，使用占位图");
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[ResourceManager] 成功加载图像: ID={imageId}, Path={usedPath}, Size={newImage.Width}x{newImage.Height}");
                    }

                    _cache[$"image:{imageId}"] = newImage;

                    ResourceLoaded?.Invoke(this, new ResourceLoadedEventArgs
                    {
                        ResourceId = imageId,
                        ResourceType = ResourceType.Image,
                        Size = newImage.Width * newImage.Height * 4, // 估算大小
                        LoadDuration = TimeSpan.FromMilliseconds(50),
                        FromCache = false
                    });

                    return newImage;
                }
                finally
                {
                    _resourceLocator.Restore();
                }
            });
        }

        /// <summary>
        /// 获取颜色空间资源
        /// </summary>
        /// <param name="colorSpaceId">颜色空间ID</param>
        /// <returns>颜色空间对象</returns>
        public async Task<ColorSpace> GetColorSpaceAsync(string colorSpaceId)
        {
            return await Task.Run(() =>
            {
                if (_cache.TryGetValue($"colorspace:{colorSpaceId}", out var cached) && cached is ColorSpace colorSpace)
                {
                    return colorSpace;
                }

                var newColorSpace = new ColorSpace
                {
                    Id = colorSpaceId,
                    Type = ColorSpaceType.RGB
                };
                _cache[$"colorspace:{colorSpaceId}"] = newColorSpace;

                ResourceLoaded?.Invoke(this, new ResourceLoadedEventArgs
                {
                    ResourceId = colorSpaceId,
                    ResourceType = ResourceType.ColorSpace,
                    Size = 512,
                    LoadDuration = TimeSpan.FromMilliseconds(5),
                    FromCache = false
                });

                return newColorSpace;
            });
        }

        /// <summary>
        /// 预加载指定资源
        /// </summary>
        /// <param name="resourceIds">资源ID列表</param>
        /// <returns>预加载结果</returns>
        public async Task<PreloadResult> PreloadResourcesAsync(IEnumerable<string> resourceIds)
        {
            var result = new PreloadResult();
            var startTime = DateTime.UtcNow;

            foreach (var resourceId in resourceIds)
            {
                try
                {
                    // 尝试预加载不同类型的资源
                    // TODO: 根据实际的资源类型进行加载
                    if (resourceId.StartsWith("font_"))
                    {
                        await GetFontAsync(resourceId);
                    }
                    else if (resourceId.StartsWith("image_"))
                    {
                        await GetImageAsync(resourceId);
                    }
                    else
                    {
                        await GetColorSpaceAsync(resourceId);
                    }

                    result.SuccessCount++;
                }
                catch
                {
                    result.FailureCount++;
                    result.FailedResources.Add(resourceId);
                }
            }

            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }

        /// <summary>
        /// 清理指定类型的缓存
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="olderThan">清理早于指定时间的缓存</param>
        /// <returns>清理的资源数量</returns>
        public async Task<int> ClearCacheAsync(ResourceType? resourceType = null, DateTime? olderThan = null)
        {
            return await Task.Run(() =>
            {
                var keysToRemove = new List<string>();

                foreach (var key in _cache.Keys)
                {
                    bool shouldRemove = true;

                    if (resourceType.HasValue)
                    {
                        var typePrefix = resourceType.Value.ToString().ToLower();
                        shouldRemove = key.StartsWith($"{typePrefix}:");
                    }

                    if (shouldRemove)
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (_cache.TryGetValue(key, out var resource) && resource is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _cache.Remove(key);
                }

                return keysToRemove.Count;
            });
        }

        /// <summary>
        /// 获取资源使用报告
        /// </summary>
        /// <returns>资源使用情况</returns>
        public async Task<ResourceUsageReport> GetUsageReportAsync()
        {
            return await Task.Run(() =>
            {
                var report = new ResourceUsageReport
                {
                    GeneratedAt = DateTime.UtcNow,
                    CachedResourceCount = _cache.Count
                };

                long totalMemory = 0;
                var typeStats = new Dictionary<ResourceType, ResourceTypeStats>();

                foreach (var kvp in _cache)
                {
                    var key = kvp.Key;
                    var resource = kvp.Value;

                    ResourceType type = ResourceType.Other;
                    if (key.StartsWith("font:"))
                        type = ResourceType.Font;
                    else if (key.StartsWith("image:"))
                        type = ResourceType.Image;
                    else if (key.StartsWith("colorspace:"))
                        type = ResourceType.ColorSpace;

                    if (!typeStats.ContainsKey(type))
                    {
                        typeStats[type] = new ResourceTypeStats();
                    }

                    typeStats[type].Count++;

                    // 估算内存使用
                    long memoryUsage = EstimateMemoryUsage(resource);
                    typeStats[type].MemoryUsed += memoryUsage;
                    totalMemory += memoryUsage;
                }

                report.TotalMemoryUsed = totalMemory;
                report.TypeStatistics = typeStats;

                return report;
            });
        }

        /// <summary>
        /// 估算资源内存使用量
        /// </summary>
        private long EstimateMemoryUsage(object resource)
        {
            return resource switch
            {
                Font => 1024,
                Bitmap bitmap => bitmap.Width * bitmap.Height * 4, // 假设RGBA
                ColorSpace => 512,
                _ => 256
            };
        }
    }
}
