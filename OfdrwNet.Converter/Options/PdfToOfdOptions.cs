using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using OfdrwNet.Core.BasicStructure.Ofd.DocInfo;

// 高级转换特性选项 (Phase 3.4 Integration - T073)
using OfdrwNet.Converter.ColorManagement;
using OfdrwNet.Converter.Recognition;
using OfdrwNet.Converter.Batch;
using OfdrwNet.Converter.Validation;
using OfdrwNet.Converter.Reporting;

// T075: 表单服务注入
using OfdrwNet.Abstractions.Forms;
using OfdrwNet.Converter.Forms;
using OfdrwNet.Converter.Scripting;
using OfdrwNet.Converter.Interaction;

namespace OfdrwNet.Converter.Options;

/// <summary>
/// PDF 到 OFD 转换选项配置类
/// </summary>
public class PdfToOfdOptions
{
    /// <summary>提取并嵌入字体（第1阶段目标）</summary>
    public bool ExtractAndEmbedFonts { get; set; } = true;

    /// <summary>提取文本</summary>
    public bool ExtractText { get; set; } = true;

    /// <summary>提取图片</summary>
    public bool ExtractImage { get; set; } = true;

    /// <summary>提取矢量路径（线条、形状等）</summary>
    public bool ExtractVector { get; set; } = true;

    /// <summary>提取注释/批注</summary>
    public bool ExtractAnnotations { get; set; } = true;

    /// <summary>提取表单</summary>
    public bool ExtractForms { get; set; } = true;

    /// <summary>逐字符精确定位（预留第2阶段）</summary>
    public bool PerGlyphPositioning { get; set; } = true;

    /// <summary>进度报告回调</summary>
    public IProgress<(int done, int total)>? Progress { get; set; }

    /// <summary>取消标记</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>规范化子集字体名称</summary>
    public bool NormalizeSubsetFontName { get; set; } = true;

    /// <summary>启用增量X位置调整</summary>
    public bool EnableDeltaX { get; set; } = true;

    /// <summary>日志记录器</summary>
    public ILogger? Logger { get; set; }

    /// <summary>是否输出真实图片资源</summary>
    public bool RealImageEmbedding { get; set; } = true;

    /// <summary>页面过滤器</summary>
    public Func<int, bool>? PageFilter { get; set; }

    /// <summary>PDF 密码</summary>
    public string? Password { get; set; }

    /// <summary>最大并行度，1表示顺序处理，>1表示并行处理</summary>
    public int MaxDegreeOfParallelism { get; set; } = 1;

    /// <summary>忽略中文字体 CMap 错误，默认启用</summary>
    public bool IgnoreCMapErrors { get; set; } = true;

    #region 文本处理选项

    /// <summary>是否在聚合后再按空格拆分文本块（用于需要保留词级定位的场景）。默认 false。</summary>
    public bool SplitTextBySpace { get; set; } = true;

    /// <summary>仅对主要由拉丁字母/数字组成的行进行按空格词级拆分；如果检测到行内包含 CJK 则回退为整行聚合。默认 true 以避免中文被错误拆分。</summary>
    public bool OnlySplitLatinWords { get; set; } = true;

    /// <summary>触发将 gap 视为"至少一个空格"的水平距离阈值比例（相对于参考字体大小）。默认 0.55。</summary>
    public double GapSpaceTriggerRatio { get; set; } = 0.55d;

    /// <summary>单个 gap 允许合成的最大空格数量上限，避免超大 gap 生成过多占位。默认 4。</summary>
    public int MaxSyntheticSpacesPerGap { get; set; } = 4;

    /// <summary>合成空格时必须达到的最小间隙（mm）。用于避免数字等窄字符被错误拆分。默认 0.45mm。</summary>
    public double MinGapForSyntheticSpaceMm { get; set; } = 0.45d;

    /// <summary>允许吸收的最大负间距（mm）。用于忽略 PDF 中的轻微负 kerning，避免错误地回填空格。默认 0.25mm。</summary>
    public double MaxNegativeKerningAbsorbMm { get; set; } = 0.25d;

    /// <summary>当 gap 发生在主要由数字、连字符等组成的片段之间时，额外放大的触发系数。数值越大，越不容易在数字间合成空格。默认 1.3。</summary>
    public double NumericGapMultiplier { get; set; } = 1.3d;

    /// <summary>数字段之间触发合成空格所需的最小实际间距（mm）。默认 1.0mm。</summary>
    public double NumericMinGapMm { get; set; } = 1.0d;

    /// <summary>启用后输出词级调试：包含字符起点/宽度、gap 判定与最终词矩形。默认 false。</summary>
    public bool EnableDebugWordLayout { get; set; } = false;

    /// <summary>对主要为 CJK 的文本，将宽度强制扩展到 字数 * 字号（避免 PDF 原 descent 线估算偏小导致截断）。默认 true。</summary>
    public bool ExpandCjkWidth { get; set; } = true;

    /// <summary>在扩展 CJK 宽度时额外增加的右侧余量比例 (相对单字宽)，默认 0.12。</summary>
    public double CjkExtraAdvanceRatio { get; set; } = 0.12d;

    /// <summary>CJK 主体行（检测到大量中文且 ASCII 字母比例低）用于合成半角空格的 gap 触发比例。默认 0.45。</summary>
    public double CjkGapTriggerRatio { get; set; } = 0.45d;

    #endregion

    #region 图片处理选项

    /// <summary>图片叠放顺序策略（默认 Sequence：后添加覆盖前添加）。可选：Sequence / YAscending / YDescending。</summary>
    public string ImageOrdering { get; set; } = "Sequence";

    /// <summary>将接近白色(#FFFFFF)背景像素转换为透明。默认 false 不处理。</summary>
    public bool MakeWhiteBackgroundTransparent { get; set; } = true;

    /// <summary>认为是"白色"的阈值(0-255)。像素 R/G/B 全部 >= 此值则视为白。默认 250。</summary>
    public byte WhiteThreshold { get; set; } = 250;

    /// <summary>透明化后若整体透明像素比例 >= 此值(0-1) 且原图无 Alpha，则自动保留一层最外框 1px 边界不透明（防止全透明消失）。默认 0.98。</summary>
    public double PreserveBorderIfAlmostAllTransparentRatio { get; set; } = 0.98;

    /// <summary>仅当图片本身无 Alpha 通道时才尝试转换；否则如果已有 Alpha 则不再二次抹白。默认 true。</summary>
    public bool OnlyIfOpaque { get; set; } = true;

    /// <summary>若图像像素格式包含 Alpha 通道，但所有像素 A 均为 255，且设置了 OnlyIfOpaque=true，则可将其视为"无 Alpha"继续做白底转透明。默认 true。</summary>
    public bool TreatFullAlphaAsOpaque { get; set; } = true;

    /// <summary>调试：在资源写出后重新读取图片并统计透明像素比例，输出日志。[默认 false] 仅用于定位透明丢失问题，会增加 I/O 开销。</summary>
    public bool DebugVerifyOutputImageAlpha { get; set; } = false;

    /// <summary>
    /// 如果为 true，则使用最简单的路径：仅将页面上的图片作为 OFD 图片资源输出（不提取文本、矢量或执行 OCR）。
    /// 这个开关用于处理纯扫描页或希望快速导出图片内容的场景。默认 false。
    /// </summary>
    public bool ExportPageImagesOnly { get; set; } = false;

    /// <summary>
    /// 当需要对不包含内嵌图像对象的页面进行光栅化（作为回退）时，指定光栅化的目标 DPI。默认 300。
    /// 仅当后端实现了页面光栅化回退时生效。本选项在本次最简实现中暂未强制使用，但为未来扩展保留。
    /// </summary>
    public int RasterizeDpi { get; set; } = 300;

    /// <summary>
    /// 如果为 true，则在转换前自动检测 PDF 是否仅包含图片（无文本、无向量路径）。
    /// 若检测为仅图片，转换器会自动采用只导出图片的最简路径。默认 false。
    /// </summary>
    public bool AutoDetectImageOnly { get; set; } = true;

    #endregion

    #region 兼容性属性

    /// <summary>启用图片提取（兼容性属性）</summary>
    public bool EnableImageExtraction
    {
        get => ExtractImage;
        set => ExtractImage = value;
    }

    /// <summary>启用注释提取（兼容性属性）</summary>
    public bool EnableAnnotationExtraction
    {
        get => ExtractAnnotations;
        set => ExtractAnnotations = value;
    }

    /// <summary>启用表单提取（兼容性属性）</summary>
    public bool EnableFormExtraction
    {
        get => ExtractForms;
        set => ExtractForms = value;
    }

    #endregion

    #region 文档信息选项

    /// <summary>输出 OFD 文档的版本号（默认 null 使用系统默认值）。</summary>
    public string? TargetOfdVersion { get; set; }

    /// <summary>允许调用方注入 DocInfo 配置。</summary>
    public Action<CtDocInfo>? ConfigureDocInfo { get; set; }

    /// <summary>是否自动生成 DocID（默认 true）。</summary>
    public bool AutoGenerateDocId { get; set; } = true;

    /// <summary>显式覆盖 DocID（为空时不覆盖）。</summary>
    public string? OverrideDocId { get; set; }

    /// <summary>当为 true 时，在写入 DocInfo 前移除现有 DocID（与 AutoGenerateDocId=false 配合使用）。</summary>
    public bool RemoveDocId { get; set; }

    /// <summary>覆盖文档标题（DocInfo/Title）。</summary>
    public string? DocTitle { get; set; }

    /// <summary>覆盖作者（DocInfo/Author）。</summary>
    public string? DocAuthor { get; set; }

    /// <summary>覆盖主题（DocInfo/Subject）。</summary>
    public string? DocSubject { get; set; }

    /// <summary>覆盖关键词（DocInfo/Keywords 原始文本）。</summary>
    public string? DocKeywords { get; set; }

    /// <summary>覆盖创建应用程序（DocInfo/Creator）。</summary>
    public string? DocCreator { get; set; }

    /// <summary>覆盖创建应用程序版本（DocInfo/CreatorVersion）。</summary>
    public string? DocCreatorVersion { get; set; }

    /// <summary>直接设置 DocInfo/CreationDate 的原始字符串（例如 PDF 的 D: 格式）。</summary>
    public string? DocCreationDateRaw { get; set; }

    /// <summary>直接设置 DocInfo/ModDate 的原始字符串。</summary>
    public string? DocModDateRaw { get; set; }

    #endregion

    #region 高级转换特性选项 (Phase 3.4 Integration - T073)

    /// <summary>启用表格识别（默认 false）。</summary>
    public bool EnableTableRecognition { get; set; } = false;

    /// <summary>启用公式识别（默认 false）。</summary>
    public bool EnableFormulaRecognition { get; set; } = false;

    /// <summary>启用颜色精度验证（ΔE检查，默认 false）。</summary>
    public bool EnableColorValidation { get; set; } = false;

    /// <summary>转换后验证OFD结构（默认 false）。</summary>
    public bool EnableValidation { get; set; } = false;

    /// <summary>RGB颜色精度阈值（ΔE，默认 2.0）。</summary>
    public double DeltaEThreshold { get; set; } = 2.0;

    /// <summary>CMYK颜色精度阈值（ΔE，默认 5.0）。</summary>
    public double CmykDeltaEThreshold { get; set; } = 5.0;

    /// <summary>兼容性配置文件名称（可选，例如 "Suwell 9.x"）。</summary>
    public string? CompatibilityProfile { get; set; }

    /// <summary>输出转换报告路径（可选，JSON格式）。</summary>
    public string? ReportPath { get; set; }

    /// <summary>启用版本控制（默认 false）。</summary>
    public bool EnableVersioning { get; set; } = false;

    /// <summary>内存警告阈值（MB，默认 2000）。</summary>
    public double MemoryWarningThresholdMB { get; set; } = 2000;

    /// <summary>内存严重阈值（MB，默认 3000）。</summary>
    public double MemoryCriticalThresholdMB { get; set; } = 3000;

    #endregion

    #region 服务注入 (T073集成)

    /// <summary>颜色空间转换器 (可选)。用于RGB/CMYK → sRGB转换并验证色差(ΔE)</summary>
    public ColorSpaceConverter? ColorConverter { get; set; }

    /// <summary>表格识别器 (可选)。用于从文本中识别表格结构</summary>
    public RuleBasedTableRecognizer? TableRecognizer { get; set; }

    /// <summary>公式识别器 (可选)。用于识别数学公式</summary>
    public BasicFormulaRecognizer? FormulaRecognizer { get; set; }

    /// <summary>内存监控器 (可选)。用于在转换过程中监控内存使用</summary>
    public MemoryGuard? MemoryGuard { get; set; }

    /// <summary>验证引擎 (可选)。用于对生成的OFD进行验证</summary>
    public CompositeValidationEngine? Validator { get; set; }

    /// <summary>错误报告构建器 (可选)。用于生成验证报告</summary>
    public ErrorReportBuilder? ReportBuilder { get; set; }

    #endregion

    #region T075: 表单服务注入

    /// <summary>表单字段映射器 (可选)。用于PDF表单字段到OFD的映射</summary>
    public IFormFieldMapper? FormMapper { get; set; }

    /// <summary>XFA检测器 (可选)。用于检测和处理XFA表单</summary>
    public XfaDetector? XfaDetector { get; set; }

    /// <summary>XFA提示写入器 (可选)。用于写入XFA降级提示</summary>
    public XfaHintWriter? XfaHintWriter { get; set; }

    /// <summary>JavaScript扫描器 (可选)。用于扫描表单中的JavaScript</summary>
    public JavaScriptScanner? JavaScriptScanner { get; set; }

    #endregion

    #region T076: 注释/交互服务注入

    /// <summary>书签转换器 (可选)。用于PDF书签转OFD书签</summary>
    public BookmarkConverter? BookmarkConverter { get; set; }

    /// <summary>动作映射器 (可选)。用于PDF动作到OFD动作的映射</summary>
    public ActionMapper? ActionMapper { get; set; }

    #endregion
}
