using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Render.KaedePhi;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验渲染配置、目标索引和位图安全边界。
/// </summary>
public static class KpcRenderValidator
{
    private const double MaximumPixelsPerBeat = 10_000d;
    private const int MaximumChannelWidth = 10_000;
    private const int MaximumSamplesPerEvent = 4096;
    private const int MaximumBeatSubdivisions = 128;
    private const double DefaultMinimumChartBeats = 4d;
    private const double MaximumChartBeats = 1_000_000d;
    private const int RenderedChannelCount = 5;
    private const int ChannelGapCount = RenderedChannelCount - 1;
    private const int AdditionalHorizontalPadding = 8;

    /// <summary>
    /// 允许的最大渲染像素总数。
    /// </summary>
    public const long MaximumRenderPixels = 200_000_000L;

    /// <summary>
    /// 校验渲染配置、索引和最终位图尺寸。
    /// </summary>
    /// <param name="chart">待渲染谱面。</param>
    /// <param name="options">渲染配置。</param>
    /// <param name="lineIndex">可选判定线索引。</param>
    /// <param name="layerIndex">可选事件层索引。</param>
    /// <returns>无返回值。</returns>
    public static void Validate(
        Chart chart,
        KpcRenderOptions options,
        int? lineIndex = null,
        int? layerIndex = null
    )
    {
        ArgumentNullException.ThrowIfNull(chart);
        ValidateOptions(options);
        ValidateSelectedIndexes(chart, lineIndex, layerIndex);

        var totalBeats = GetTotalBeats(chart);
        ValidateTotalBeats(totalBeats);
        var (width, height) = CalculateBitmapSize(options, totalBeats);
        ValidateBitmapSize(width, height);
    }

    /// <summary>
    /// 校验不依赖具体谱面的渲染配置。
    /// </summary>
    /// <param name="options">渲染配置。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateOptions(KpcRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateSamplingOptions(options);
        ValidateLayoutOptions(options);
        ValidateRangeOptions(options);
    }

    /// <summary>
    /// 校验单个事件层与渲染配置，并验证渲染位图尺寸的安全边界。
    /// </summary>
    /// <param name="layer">待渲染的事件层。</param>
    /// <param name="options">渲染配置。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateEventLayer(KpcEvents.EventLayer layer, KpcRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ValidateOptions(options);

        var totalBeats = GetLayerTotalBeats(layer);
        ValidateTotalBeats(totalBeats);
        var (width, height) = CalculateBitmapSize(options, totalBeats);
        ValidateBitmapSize(width, height);
    }

    private static double GetLayerTotalBeats(KpcEvents.EventLayer layer)
    {
        var totalBeats = DefaultMinimumChartBeats;
        totalBeats = UpdateMaximumEndBeat(totalBeats, layer.MoveXEvents);
        totalBeats = UpdateMaximumEndBeat(totalBeats, layer.MoveYEvents);
        totalBeats = UpdateMaximumEndBeat(totalBeats, layer.RotateEvents);
        totalBeats = UpdateMaximumEndBeat(totalBeats, layer.AlphaEvents);
        totalBeats = UpdateMaximumEndBeat(totalBeats, layer.SpeedEvents);
        return totalBeats;
    }

    private static void ValidateSamplingOptions(KpcRenderOptions options)
    {
        ValidateFinitePositiveAtMost(
            options.PixelsPerBeat,
            MaximumPixelsPerBeat,
            nameof(options.PixelsPerBeat)
        );
        ValidatePositiveAtMost(
            options.ChannelWidth,
            MaximumChannelWidth,
            nameof(options.ChannelWidth)
        );
        ValidatePositiveAtMost(
            options.SamplesPerEvent,
            MaximumSamplesPerEvent,
            nameof(options.SamplesPerEvent)
        );
        ValidatePositiveAtMost(
            options.BeatSubdivisions,
            MaximumBeatSubdivisions,
            nameof(options.BeatSubdivisions)
        );
    }

    private static void ValidateLayoutOptions(KpcRenderOptions options)
    {
        if (
            options.LeftMargin < 0
            || options.HeaderHeight < 0
            || options.BottomPadding < 0
            || options.ChannelPadding < 0
            || options.StrokeWidth < 0
        )
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    private static void ValidateRangeOptions(KpcRenderOptions options)
    {
        ValidateFiniteNonNegative(options.RangePaddingRatio, nameof(options.RangePaddingRatio));
        ValidatePositiveAtMost(
            options.RangeSamplesPerEvent,
            MaximumSamplesPerEvent,
            nameof(options.RangeSamplesPerEvent)
        );
        ValidateFiniteNonNegative(
            options.SegmentGroupTolerance,
            nameof(options.SegmentGroupTolerance)
        );
        ValidateFiniteNonNegative(options.MinValueRangeHalf, nameof(options.MinValueRangeHalf));
        ValidateFiniteNonNegative(
            options.MinValueRangeHalfRatio,
            nameof(options.MinValueRangeHalfRatio)
        );
    }

    private static void ValidateSelectedIndexes(Chart chart, int? lineIndex, int? layerIndex)
    {
        var judgeLines = chart.JudgeLineList ?? [];
        if (lineIndex is < 0 || lineIndex >= judgeLines.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));
        if (layerIndex is not null && lineIndex is null)
            throw new ArgumentException(
                "指定事件层索引时必须同时指定判定线索引。",
                nameof(layerIndex)
            );
        if (lineIndex is not null && layerIndex is not null)
            ValidateLayerIndex(judgeLines[lineIndex.Value], layerIndex.Value);
    }

    private static void ValidateLayerIndex(JudgeLine line, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= line.EventLayers.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
    }

    private static double GetTotalBeats(Chart chart)
    {
        var totalBeats = DefaultMinimumChartBeats;
        foreach (var line in chart.JudgeLineList)
        {
            if (line is null || line.EventLayers is null)
                continue;
            foreach (var layer in line.EventLayers)
            {
                if (layer is null)
                    continue;
                totalBeats = UpdateMaximumEndBeat(totalBeats, layer.MoveXEvents);
                totalBeats = UpdateMaximumEndBeat(totalBeats, layer.MoveYEvents);
                totalBeats = UpdateMaximumEndBeat(totalBeats, layer.RotateEvents);
                totalBeats = UpdateMaximumEndBeat(totalBeats, layer.AlphaEvents);
                totalBeats = UpdateMaximumEndBeat(totalBeats, layer.SpeedEvents);
            }
        }

        return totalBeats;
    }

    private static double UpdateMaximumEndBeat<T>(
        double currentMaximum,
        IEnumerable<KpcEvents.Event<T>>? events
    )
        where T : notnull
    {
        if (events is null)
            return currentMaximum;

        foreach (var chartEvent in events)
        {
            var endBeat = (double)chartEvent.EndBeat;
            if (!double.IsFinite(endBeat))
                throw new FormatException("谱面事件拍数必须是有限数值。");
            if (endBeat > currentMaximum)
                currentMaximum = endBeat;
        }

        return currentMaximum;
    }

    private static void ValidateTotalBeats(double totalBeats)
    {
        if (totalBeats is < 0 or > MaximumChartBeats)
            throw new ArgumentOutOfRangeException("chart", "谱面总拍数超过安全上限。");
    }

    private static (long Width, long Height) CalculateBitmapSize(
        KpcRenderOptions options,
        double totalBeats
    )
    {
        var heightValue =
            options.HeaderHeight
            + Math.Ceiling(totalBeats * options.PixelsPerBeat)
            + options.BottomPadding;
        if (!double.IsFinite(heightValue) || heightValue > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(options), "渲染位图高度超过安全上限。");

        var width =
            options.LeftMargin
            + (long)RenderedChannelCount * options.ChannelWidth
            + (long)ChannelGapCount * options.ChannelPadding
            + AdditionalHorizontalPadding;
        return (width, (long)heightValue);
    }

    private static void ValidateBitmapSize(long width, long height)
    {
        if (
            height <= 0
            || width <= 0
            || width > int.MaxValue
            || width * height > MaximumRenderPixels
        )
            throw new ArgumentOutOfRangeException("options", "渲染位图尺寸超过安全上限。");
    }

    private static void ValidateFinitePositiveAtMost(
        double value,
        double maximum,
        string parameterName
    )
    {
        if (!double.IsFinite(value) || value <= 0 || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidatePositiveAtMost(int value, int maximum, string parameterName)
    {
        if (value <= 0 || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateFiniteNonNegative(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
