# OFDRW.NET

> 面向 .NET 的 OFD (Open Fixed-layout Document) 文档读写、转换与工具套件。
> 支持：PDF→OFD 转换、OFD→PDF 导出、OFD 文档读取与结构分析、命令行批处理、白底转透明、字体抽取、图像顺序控制等。

## 功能概览

| 模块 | 说明 | 项目/命名空间 |
|------|------|---------------|
| PDF → OFD 转换 | 解析 PDF（iText7）提取文本/图片/字体/路径，生成 OFD 结构 | `OfdrwNet.Converter` |
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

```powershell
# PDF 转 OFD
ofdrw convert -i .\in.pdf -o .\out-ofd-dir --parallel 4 --alpha-white true --white-threshold 250

# 调试 OFD 结构
ofdrw debug -f .\out.ofd -v

# 扫描输出目录透明像素
ofdrw alpha-scan -d .\out-ofd-dir\Doc_0\Res --sample-step 60
```

常用选项说明：
- `--alpha-white/--alpha-white-to-transparent` 启用白底转透明（默认内部 true）
- `--white-threshold` 白色判定阈值 (0-255)
- `--only-if-opaque` 仅无 Alpha 才处理（默认 true）
- `--force-alpha-white` 强制抹白，即使已有 Alpha
- `--parallel/--threads` 并行度
- `--real-image-embedding` 直接嵌入原始图片
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

### 性能建议
- 大文档：`--parallel` 使用逻辑核心一半或全核
- 关闭不需要的提取：表单/注释
- 避免开启逐字定位
- 降低 DPI（导出 PDF 时）

## 目录结构（核心）
```
src/
  OfdrwNet.Core/          # 规范结构与基础类型
  OfdrwNet.Reader/        # 读取/解析/导航
  OfdrwNet.Converter/     # PDF<->OFD 转换
  OfdrwNet.Cli/           # 命令行
  OfdrwNet.Abstractions/  # 抽象与共享模型
  OfdrwNet.Graphics/
  OfdrwNet.Layout/
```

## FAQ
| 问题 | 说明 |
|------|------|
| 输出文字错位 | 检查是否关闭 DeltaX 或字体缺失 |
| 透明未生效 | 是否已有 Alpha 且未加 `--force-alpha-white`；阈值是否过低 |
| 性能慢 | 降低 DPI / 关闭逐字定位 / 调整并行 |
| 页面尺寸不对 | 依赖 PDF 第一页；多尺寸暂未逐页覆盖 |

## 端到端示例
```powershell
# 1. PDF -> OFD
ofdrw convert -i sample.pdf -o out-ofd -v --alpha-white true --white-threshold 250

# 2. OFD -> PDF (示例，确保项目已构建)
# 假设已有编译产物引用方式，或在代码中调用 ConvertHelper.ToPdf
```

## 贡献
- 提交 Issue（性能 / 规范兼容 / 错误）
- 提供复杂 PDF/OFD 作为测试样本
- PR 前请先讨论大的模型或结构变更

## License
(根据实际授权补充)

---
如需更详细的 API 参考，请查看 `docs/API_Documentation.md`。
