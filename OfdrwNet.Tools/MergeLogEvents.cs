using Microsoft.Extensions.Logging;

namespace OfdrwNet.Tools;

/// <summary>
/// 合并器日志事件常量集中定义。
/// 拆分至独立文件，避免 OFDMerger 过于臃肿。
/// </summary>
public static class MergeLogEvents
{
    public static readonly EventId Start = new(1000, "Merge.Start");
    public static readonly EventId End = new(1001, "Merge.End");
    public static readonly EventId Stats = new(1002, "Merge.Stats");
    public static readonly EventId Error = new(1003, "Merge.Error");
    public static readonly EventId PageIndexWarn = new(1004, "Merge.PageIndexWarn");
    public static readonly EventId PageSize = new(1010, "Merge.PageSize");
    public static readonly EventId PageSizeFail = new(1011, "Merge.PageSizeFail");
    public static readonly EventId Page = new(1020, "Merge.Page");
    public static readonly EventId PageMix = new(1025, "Merge.PageMix");
    public static readonly EventId PageMixTodo = new(1026, "Merge.PageMixTodo");
    public static readonly EventId ResAttrError = new(1031, "Merge.ResAttrError");
    public static readonly EventId ResError = new(1032, "Merge.ResError");
    public static readonly EventId CopyResFail = new(1033, "Merge.CopyResFail");
    public static readonly EventId PublicRes = new(1035, "Merge.PublicRes");
    public static readonly EventId PublicResFail = new(1036, "Merge.PublicResFail");
    public static readonly EventId ClipRiskFail = new(1040, "Merge.ClipRiskFail");
    public static readonly EventId ReassignIdFail = new(1045, "Merge.ReassignIdFail");
    public static readonly EventId Canceled = new(1099, "Merge.Canceled");
}
