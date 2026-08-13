using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace KaedePhi.Tool.Analyzers.Analysis;

/// <summary>
/// KpcRenderOptions 类型的识别与数值属性的合法取值区间。
/// </summary>
internal static class RenderOptionsApi
{
    private const string RenderNamespace = "KaedePhi.Tool.Render.KaedePhi";
    private const string RenderOptionsMetadataName = "KpcRenderOptions";

    /// <summary>
    /// 描述数值属性的合法取值区间。
    /// </summary>
    /// <param name="Min">最小允许值</param>
    /// <param name="MinExclusive">最小值是否开区间（为 true 时不允许等于 Min）</param>
    /// <param name="Max">最大允许值；double.MaxValue 表示无上限</param>
    /// <param name="Display">用于诊断消息的区间文本</param>
    internal readonly struct Bound
    {
        public Bound(double min, bool minExclusive, double max, string display)
        {
            Min = min;
            MinExclusive = minExclusive;
            Max = max;
            Display = display;
        }

        public double Min { get; }

        public bool MinExclusive { get; }

        public double Max { get; }

        public string Display { get; }
    }

    // 各数值属性的合法区间，与 ChartProcessingValidator.ValidateRender 保持一致
    private static readonly ImmutableDictionary<string, Bound> Bounds =
        ImmutableDictionary.CreateRange([
            // 每拍像素高：大于 0 且不超过 10000
            new KeyValuePair<string, Bound>("PixelsPerBeat", new(0, true, 10_000, "(0, 10000]")),
            // 通道宽：大于 0 且不超过 10000
            new KeyValuePair<string, Bound>("ChannelWidth", new(0, true, 10_000, "(0, 10000]")),
            // 事件采样点：大于 0 且不超过 4096
            new KeyValuePair<string, Bound>("SamplesPerEvent", new(0, true, 4096, "(0, 4096]")),
            // 每拍细分格线：大于 0 且不超过 128
            new KeyValuePair<string, Bound>("BeatSubdivisions", new(0, true, 128, "(0, 128]")),
            // 值域探测采样点：大于 0 且不超过 4096
            new KeyValuePair<string, Bound>(
                "RangeSamplesPerEvent",
                new(0, true, 4096, "(0, 4096]")
            ),
            // 以下属性仅要求非负
            new KeyValuePair<string, Bound>(
                "LeftMargin",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "HeaderHeight",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "BottomPadding",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "ChannelPadding",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "StrokeWidth",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "RangePaddingRatio",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "SegmentGroupTolerance",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "MinValueRangeHalf",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
            new KeyValuePair<string, Bound>(
                "MinValueRangeHalfRatio",
                new(0, false, double.MaxValue, "[0, \u221e)")
            ),
        ]);

    /// <summary>
    /// 尝试获取渲染配置属性的合法取值区间。
    /// </summary>
    /// <param name="property">待检查的属性符号</param>
    /// <param name="bound">属性对应的取值区间</param>
    /// <returns>是否为受支持的渲染配置数值属性</returns>
    public static bool TryGetBound(IPropertySymbol property, out Bound bound)
    {
        bound = default;
        if (!IsRenderOptionsType(property.ContainingType))
            return false;
        return Bounds.TryGetValue(property.Name, out bound);
    }

    /// <summary>
    /// 判断类型是否为 KpcRenderOptions。
    /// </summary>
    /// <param name="type">待判断的类型符号</param>
    /// <returns>是否为 KpcRenderOptions</returns>
    private static bool IsRenderOptionsType(INamedTypeSymbol? type) =>
        type is not null
        && type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == RenderNamespace
        && type.OriginalDefinition.MetadataName == RenderOptionsMetadataName;
}
