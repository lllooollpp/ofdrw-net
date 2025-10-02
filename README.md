# OFDRW.NET

> 面向 .NET 的 OFD (Open Fixed-layout Document) 文档读写、转换与工具套件。
> 支持：PDF→OFD 转换、OFD→PDF 导出、OFD 文档读取与结构分析、命令行批处理、白底转透明、字体抽取、图像顺序控制等。

**🆕 高级转换特性 (v2.0)**:
- 🎨 **颜色管理**: RGB/CMYK/ICC色彩空间转换, ΔE精度验证 (ΔE < 2.0 for RGB)
- 📊 **表格与公式识别**: 自动检测PDF表格结构, LaTeX公式提取
- 📝 **表单与交互**: PDF表单字段映射, XFA降级, 注释/书签/动作转换
- 🔒 **脚本处理**: JavaScript安全扫描与隔离执行 (QuickJS沙箱)
- 🎥 **多媒体**: 视频/音频提取与降级 (占位符/外部链接)
- 💾 **内存优化**: MemoryGuard自动调度, 批处理并行优化
- 📦 **版本控制**: 增量差异版本链, 版本合并与回滚
- 🔧 **兼容性**: 4种OFD阅读器配置文件 (Suwell/Foxit/WPS/Baseline)
- ✍️ **数字签名**: 可插拔签章适配器, SM2/SM3国密支持
- 🛡️ **安全性**: 权限配置, 加密引擎集成
- ✅ **验证**: Schema + 语义联合验证, 结构化错误报告
- 📈 **性能基准**: BenchmarkDotNet性能测试, 详细性能指标

## 功能概览

| 模块 | 说明 | 项目/命名空间 |
|------|------|---------------|
| PDF → OFD 转换 | 解析 PDF（iText7）提取文本/图片/字体/路径，生成 OFD 结构 | `OfdrwNet.Converter` |
| **颜色管理** | RGB/CMYK/ICC转换, ΔE验证 | `ColorManagement` |
| **表格/公式识别** | 基于规则的表格检测, LaTeX公式识别 | `Recognition` |
| **表单处理** | PDF表单映射, XFA降级 | `Forms` |
| **交互转换** | 注释/书签/动作转换 | `Interaction` |
| **脚本处理** | JS安全扫描, QuickJS隔离执行 | `Scripting` |
| **多媒体** | 视频/音频提取与降级 | `Media` |
| **内存管理** | MemoryGuard自动调度 | `Batch` |
| **版本控制** | 增量差异版本链 | `Versioning` |
| **兼容性** | 4种阅读器配置文件降级 | `Compatibility` |
| **数字签名** | SM2/SM3可插拔适配器 | `Sign` |
| **安全性** | 权限配置, 加密引擎 | `Security` |
| **验证** | Schema + 语义联合验证 | `Validation` |
| OFD → PDF 导出 | 将 OFD 文档渲染为 PDF 图像流再封装 | `OfdrwNet.Converter` (`PDFExporter`) |
| OFD 读取 | 加载 OFD.zip，解析结构、页面、资源 | `OfdrwNet.Reader` |
| CLI 工具 | 命令行批量转换、调试、透明度检测 | `OfdrwNet.Cli` |
| 图片处理 | 白底转透明、阈值控制、TIFF 反相 + 透明化 | `OfdrwNet` / 转换内部 Refactor 模块 |
| 字体抽取与归一 | 子集前缀去除、逻辑名映射 | `Refactor.FontExtractor` |
| 文本布局修正 | CJK 宽度补偿、DeltaX 间距修正、空格合成 | `PdfToOfdOptions` |
| 图像叠放顺序 | Sequence / YAscending / YDescending | `PdfToOfdOptions.ImageOrdering` |
| 调试与诊断 | 结构验证 / 透明像素扫描 / 资源加载 | CLI `debug` / `alpha-scan` |

## 快速开始

### 1. 通过代码执行 PDF → OFD

```csharp
using OfdrwNet.Converter;

await ConvertHelper.PdfToOfdAsync(
    pdfPath: "input.pdf",
    ofdOutputDir: "output-ofd-dir",
    options: new ConvertHelper.PdfToOfdOptions {
        ExtractAndEmbedFonts = true,
        MakeWhiteBackgroundTransparent = true,
        WhiteThreshold = 248,
        ImageOrdering = "YAscending",
        PerGlyphPositioning = false,
        EnableDeltaX = true
    }
);
```

输出目录结构示例：
```
OFD.xml
Doc_0/
  Document.xml
  Pages/
    Page_0/Content.xml
  Res/
    Image_*.png
    Font_*.font
```

### 2. 使用 CLI

构建：进入 `src/OfdrwNet.Cli` 执行 `dotnet build` 或发布。

**基础转换**:
```powershell
# PDF 转 OFD
ofdrw convert -i .\in.pdf -o .\out-ofd-dir --parallel 4 --alpha-white true --white-threshold 250

# 调试 OFD 结构
ofdrw debug -f .\out.ofd -v

# 扫描输出目录透明像素
ofdrw alpha-scan -d .\out-ofd-dir\Doc_0\Res --sample-step 60
```

**高级转换 (使用兼容性配置文件)**:
```powershell
# 使用Suwell 9.x配置文件转换
ofdrw convert -i input.pdf -o output-ofd --profile "Suwell 9.x" --downgrade-mode auto

# 使用Foxit 11.x配置文件转换
ofdrw convert -i input.pdf -o output-ofd --profile "Foxit 11.x" --downgrade-mode manual

# 验证OFD结构
ofdrw validate -f output.ofd --report validation-report.json

# 分析颜色精度
ofdrw analyze-color -f output.ofd --delta-e-threshold 2.0

# 批处理 + 内存监控
ofdrw convert-batch -i .\input-dir -o .\output-dir --parallel 4 --memory-log memory-events.json
```

常用选项说明：
- `--alpha-white/--alpha-white-to-transparent` 启用白底转透明（默认内部 true）
- `--white-threshold` 白色判定阈值 (0-255)
- `--only-if-opaque` 仅无 Alpha 才处理（默认 true）
- `--force-alpha-white` 强制抹白，即使已有 Alpha
- `--parallel/--threads` 并行度
- `--real-image-embedding` 直接嵌入原始图片
- `--profile` 兼容性配置文件 (Suwell 9.x / Foxit 11.x / WPS 2023 / Baseline 1.0)
- `--downgrade-mode` 降级模式 (auto / manual / off)
- `--enable-color-validation` 启用颜色精度验证 (ΔE检查)
- `--enable-table-recognition` 启用表格识别
- `--enable-formula-recognition` 启用公式识别
- `--memory-log` 输出内存事件日志
- `--per-glyph-positioning` 逐字定位（性能低）
- `--doc-id` 覆盖生成的 DocInfo.DocID（32 位 UUID 字符串）
- `--no-doc-id` 禁用自动生成并移除 DocID 元素
- `--doc-title/--doc-author/--doc-creator` 覆盖 DocInfo 标题、作者、创建应用
- `--doc-creator-version` 覆盖 DocInfo.CreatorVersion
- `--doc-subject/--doc-keywords` 覆盖文档主题与关键字
- `--doc-creation-date/--doc-mod-date` 直接写入 DocInfo 日期字段（保持 D: 格式）

### 3. OFD → PDF

```csharp
using OfdrwNet.Converter;

ConvertHelper.ToPdf(
    inputPath: "doc.ofd",
    outputPath: "export.pdf",
    options: new ConvertHelper.PdfExportOptions {
        Dpi = 150,
        PreserveLayout = true,
        PageFilter = p => p <= 5
    }
);
```

### 4. 读取 OFD 文档

```csharp
using OfdrwNet.Reader;

using var reader = new OfdReader("doc.ofd");
var info = await reader.GetDocumentInfoAsync();
Console.WriteLine($"Pages = {info.PageCount}");

var pages = reader.GetPageList();
var first = pages[0];
Console.WriteLine($"First page size = {first.Width}x{first.Height}mm");

var resMgr = reader.GetResourceManager();
var validation = await reader.ValidateDocumentAsync();
Console.WriteLine($"Valid = {validation.IsValid}");
```

## 关键特性说明

### 文本处理
- DeltaX 归一与 CJK 宽度扩展（`ExpandCjkWidth`, `CjkExtraAdvanceRatio`）
- 空格智能合成：英文与 CJK 使用不同 gap 判定（`GapSpaceTriggerRatio`, `CjkGapTriggerRatio`）
- 逐字定位可选（精度 vs 性能）

### 图片处理
- 白底转透明：阈值像素→A=0
- 形式 Alpha 全 255 仍可视为不透明继续处理（`TreatFullAlphaAsOpaque`）
- TIFF 先反相再透明
- 可调试验证导出后透明度（`DebugVerifyOutputImageAlpha`）

### 图像叠放顺序
`ImageOrdering`：
- `Sequence`（默认）
- `YAscending`
- `YDescending`

### 字体
- 子集前缀移除（`ABCDEF+SimSun` → `SimSun`）
- 系统字体逻辑名归一
- 可扩展映射 / 嵌入策略

### 颜色管理
- **RGB → sRGB转换**: 目标 ΔE < 2.0 (DR-18)
- **CMYK → sRGB转换**: 目标 ΔE < 5.0 (DR-19)
- **ICC配置文件转换**: 目标 ΔE < 3.0 (DR-20)
- **ΔE计算**: CIE76公式, 实时验证颜色精度
- **性能**: 1080p图像 < 100ms (RGB), < 150ms (CMYK)

### 表格与公式识别
- **表格识别**: 基于规则的自动检测, 支持合并单元格
- **公式识别**: LaTeX表达式提取, 置信度评分
- **性能**: 典型页面 < 200ms, 复杂表格(20x50) < 500ms

### 内存管理与批处理
- **MemoryGuard**: 自动内存监控, 阈值触发(warning: 2GB, critical: 3GB)
- **动作响应**: GarbageCollect / FlushToDisk / ReduceParallelism / Abort
- **批处理**: 并行调度优化, 顺序 > 5 files/s, 4x并行 > 15 files/s
- **分段建议**: 自动计算最优分段大小, 避免OOM

### 兼容性与降级
- **4种阅读器配置文件**: Suwell 9.x, Foxit 11.x, WPS 2023, Baseline 1.0
- **特性降级**: 视频→占位符, JS→移除, 3D→2D, XFA→静态表单
- **限制配置**: maxPageSize, maxFileSize, maxPages, maxAnnotations
- **详细文档**: 查看 `docs/compatibility-matrix.md`

### 验证与报告
- **Schema验证**: OFD XSD规范检查 (GB/T 33190-2016)
- **语义验证**: 引用完整性, 循环引用检测, 边界验证
- **联合验证**: CompositeValidationEngine组合检查
- **错误报告**: JSON格式转换报告, 包含errors/stats/colorDelta
- **40+错误码**: 详见 `docs/troubleshooting.md`

### 性能建议
- 大文档：`--parallel` 使用逻辑核心一半或全核
- 关闭不需要的提取：表单/注释
- 避免开启逐字定位
- 降低 DPI（导出 PDF 时）
- 启用MemoryGuard：自动调度避免OOM
- 使用兼容性配置文件：针对目标阅读器优化

### 性能基准 (BenchmarkDotNet)
运行性能测试:
```powershell
cd src/OfdrwNet.Converter.Tests
dotnet run -c Release --filter *ExtendedPerformanceBenchmarks*
```

关键指标:
- RGB转换 (1080p): < 100ms
- CMYK转换 (1080p): < 150ms
- ΔE计算: < 1μs
- 表格识别: < 200ms (典型页)
- 公式识别: < 100ms (数学页)
- 批处理吞吐: > 15 files/s (4x并行)
- MemoryGuard检查: < 1ms

## 目录结构（核心）
```
src/
  OfdrwNet.Core/          # 规范结构与基础类型
  OfdrwNet.Reader/        # 读取/解析/导航
  OfdrwNet.Converter/     # PDF<->OFD 转换 + 高级特性
    ColorManagement/      # 颜色管理 (RGB/CMYK/ICC, ΔE)
    Recognition/          # 表格与公式识别
    Forms/                # 表单处理 (XFA降级)
    Interaction/          # 注释/书签/动作转换
    Scripting/            # JavaScript安全处理
    Media/                # 多媒体提取
    Batch/                # 批处理与内存管理
    Versioning/           # 版本控制
    Compatibility/        # 兼容性降级
    Security/             # 安全性
    Validation/           # 验证
    Domain/               # 领域模型
    DependencyInjection/  # DI扩展
  OfdrwNet.Sign/          # 数字签名 (SM2/SM3)
  OfdrwNet.Cli/           # 命令行
  OfdrwNet.Abstractions/  # 抽象与共享模型
  OfdrwNet.Graphics/      # 图形处理
  OfdrwNet.Layout/        # 布局分析
  OfdrwNet.Converter.Tests/  # 单元测试 + 性能基准
    Performance/          # BenchmarkDotNet测试
```

## FAQ
| 问题 | 说明 | 解决方案 |
|------|------|---------|
| 输出文字错位 | 检查是否关闭 DeltaX 或字体缺失 | 启用 `EnableDeltaX=true`, 检查字体嵌入 |
| 透明未生效 | 是否已有 Alpha 且未加 `--force-alpha-white`；阈值是否过低 | 调整 `WhiteThreshold` 或使用 `--force-alpha-white` |
| 性能慢 | 降低 DPI / 关闭逐字定位 / 调整并行 | 使用 `--parallel=4`, 关闭 `PerGlyphPositioning` |
| 页面尺寸不对 | 依赖 PDF 第一页；多尺寸暂未逐页覆盖 | 检查源PDF页面尺寸一致性 |
| 颜色精度超标 (ΔE > 2.0) | RGB转换误差过大 | 使用ICC配置文件转换, 查看 `conversion-report.json` |
| 表格识别失败 | PDF表格结构不规则 | 调整 `RuleBasedTableRecognizer` 阈值, 或手动标注 |
| 内存不足 (OOM) | 大文档批处理内存溢出 | 启用 `MemoryGuard`, 降低 `MaxDegreeOfParallelism` |
| 视频/音频不支持 | 目标阅读器不兼容 | 使用 `--profile "Suwell 9.x"` 自动降级 |
| JavaScript被移除 | 脚本不安全 | 检查 `JavaScriptScanner` 日志, 确认安全性 |
| XFA表单丢失 | XFA不支持 | 自动降级为静态表单, 查看 `XfaHintWriter` 输出 |

详细错误码与诊断: 查看 `docs/troubleshooting.md` (40+错误码)

## 端到端示例
```powershell
# 1. PDF -> OFD (基础转换)
ofdrw convert -i sample.pdf -o out-ofd -v --alpha-white true --white-threshold 250

# 2. PDF -> OFD (高级特性 + 兼容性)
ofdrw convert -i sample.pdf -o out-ofd `
  --profile "Suwell 9.x" `
  --downgrade-mode auto `
  --enable-color-validation true `
  --enable-table-recognition true `
  --enable-formula-recognition true `
  --parallel 4 `
  --memory-log memory-events.json

# 3. 验证OFD结构
ofdrw validate -f out-ofd/output.ofd --report validation-report.json

# 4. 分析颜色精度
ofdrw analyze-color -f out-ofd/output.ofd --delta-e-threshold 2.0

# 5. OFD -> PDF (示例，确保项目已构建)
# 在代码中调用:
# ConvertHelper.ToPdf("output.ofd", "export.pdf", new PdfExportOptions { Dpi = 150 });

# 6. 批处理 + 内存监控
ofdrw convert-batch `
  -i .\input-dir `
  -o .\output-dir `
  --parallel 4 `
  --memory-log batch-memory.json `
  --report batch-report.json
```

**端到端工作流 (代码示例)**:
```csharp
using OfdrwNet.Converter;
using OfdrwNet.Converter.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// 1. 配置服务
var services = new ServiceCollection();
services.AddOfdConverter();
var sp = services.BuildServiceProvider();

// 2. 转换 PDF → OFD
await ConvertHelper.PdfToOfdAsync("input.pdf", "output-ofd");

// 3. 验证 OFD
var validator = sp.GetRequiredService<CompositeValidationEngine>();
var result = validator.Validate("output-ofd");

if (!result.IsValid)
{
    Console.WriteLine($"Validation failed: {result.TotalErrors} errors");
    foreach (var error in result.AllErrors.Take(10))
    {
        Console.WriteLine($"- [{error.Severity}] {error.Message}");
    }
}

// 4. 分析颜色精度
var converter = sp.GetRequiredService<ColorSpaceConverter>();
// ... (在转换过程中自动记录 ΔE)

// 5. 生成报告
var reportBuilder = sp.GetRequiredService<ErrorReportBuilder>();
var report = reportBuilder
    .WithJob("job-001", "input.pdf", "output-ofd")
    .WithStats(new ConversionStatistics { PagesConverted = 10 })
    .WithColorDelta(avgDeltaE: 1.5, maxDeltaE: 2.8, samplesAboveThreshold: 3)
    .Build();

reportBuilder.BuildToFile("conversion-report.json");

// 6. OFD → PDF 导出
ConvertHelper.ToPdf("output-ofd/output.ofd", "export.pdf", new PdfExportOptions
{
    Dpi = 150,
    PreserveLayout = true
});
```

## 贡献
- 提交 Issue（性能 / 规范兼容 / 错误）
- 提供复杂 PDF/OFD 作为测试样本
- PR 前请先讨论大的模型或结构变更

## 文档索引
- **API文档**: `docs/API_Documentation.md` - 完整API参考 + 高级特性使用指南
- **兼容性矩阵**: `docs/compatibility-matrix.md` - 4种阅读器配置文件, 降级规则, ΔE targets
- **故障排除**: `docs/troubleshooting.md` - 40+错误码, 诊断步骤, 常见场景
- **需求文档**: `docs/需求文档.md` - 功能需求与技术规范
- **开发指南**: `.github/copilot-instructions.md` - 项目结构与开发约定

## License
(根据实际授权补充)

---
**v2.0 新特性**: 颜色管理 | 表格/公式识别 | 表单/交互转换 | 脚本安全 | 多媒体降级 | 内存优化 | 版本控制 | 兼容性 | 数字签名 | 验证报告

如需更详细的 API 参考，请查看 `docs/API_Documentation.md`。
