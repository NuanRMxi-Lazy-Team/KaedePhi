using KaedePhi.Core.KaedePhi;
using KaedePhi.Tool.Render.KaedePhi;

// ReSharper disable MemberCanBePrivate.Global

namespace KaedePhi.Tool.Common;

/// <summary>
/// 校验谱面处理和渲染参数，避免非法输入进入高开销算法。
/// </summary>
public static class ChartProcessingValidator
{
    /// <summary>
    /// 允许的最大采样精度，需与 Beat 的可表示分母上限保持一致。
    /// </summary>
    public const double MaximumPrecision = 1024d;

    /// <summary>
    /// 允许的最大渲染像素总数。
    /// </summary>
    public const long MaximumRenderPixels = 200_000_000L;

    /// <summary>
    /// 允许读取的最大谱面文件大小。
    /// </summary>
    public const long MaximumInputBytes = 4096L * 1024 * 1024;

    /// <summary>
    /// 允许处理的最大判定线数量。
    /// </summary>
    public const int MaximumJudgeLines = 10_000;

    /// <summary>
    /// 允许处理的最大事件和音符总数。
    /// </summary>
    public const long MaximumChartItems = 1_000_000_000_000L;

    /// <summary>
    /// 校验输入文件存在且没有超过大小上限。
    /// </summary>
    /// <param name="path">输入文件路径。</param>
    public static void ValidateInputFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("输入文件路径不能为空。", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("输入谱面文件不存在。", path);
        if (info.Length > MaximumInputBytes)
            throw new IOException(
                $"输入谱面文件超过 {MaximumInputBytes / 1024 / 1024} MB 大小限制。"
            );
    }

    /// <summary>
    /// 校验采样精度。
    /// </summary>
    /// <param name="precision">每拍采样次数。</param>
    /// <param name="parameterName">参数名称。</param>
    public static void ValidatePrecision(double precision, string parameterName = "precision")
    {
        if (!double.IsFinite(precision) || precision <= 0 || precision > MaximumPrecision)
            throw new ArgumentOutOfRangeException(
                parameterName,
                precision,
                $"采样精度必须是大于 0 且不超过 {MaximumPrecision} 的有限数值。"
            );
    }

    /// <summary>
    /// 校验拟合容差。
    /// </summary>
    /// <param name="tolerance">拟合容差。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateTolerance(double tolerance, string parameterName = "tolerance")
    {
        if (!double.IsFinite(tolerance) || tolerance < 0 || tolerance > 100)
            throw new ArgumentOutOfRangeException(
                parameterName,
                tolerance,
                "拟合容差必须是 0 到 100 之间的有限数值。"
            );
    }

    /// <summary>
    /// 校验父子线索引并检测父线循环。
    /// </summary>
    /// <param name="judgeLines">待检查的判定线列表。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateJudgeLineHierarchy(IReadOnlyList<JudgeLine> judgeLines)
    {
        ArgumentNullException.ThrowIfNull(judgeLines);
        if (judgeLines.Count > MaximumJudgeLines)
            throw new FormatException("谱面判定线数量超过安全上限。");

        long itemCount = 0;
        foreach (var line in judgeLines)
        {
            if ((object?)line is null || line.EventLayers is null || line.Notes is null)
                throw new FormatException("谱面包含空的判定线或事件集合。");
            itemCount += line.Notes.Count;
            foreach (var layer in line.EventLayers)
            {
                if ((object?)layer is null)
                    throw new FormatException("谱面包含空的事件层。");
                itemCount +=
                    (layer.MoveXEvents?.Count ?? 0)
                    + (layer.MoveYEvents?.Count ?? 0)
                    + (layer.RotateEvents?.Count ?? 0)
                    + (layer.AlphaEvents?.Count ?? 0)
                    + (layer.SpeedEvents?.Count ?? 0);
                if (itemCount > MaximumChartItems)
                    throw new FormatException("谱面事件和音符数量超过安全上限。");
            }
        }

        for (var index = 0; index < judgeLines.Count; index++)
        {
            var father = judgeLines[index].Father;
            if (father < -1 || father >= judgeLines.Count)
                throw new FormatException($"判定线 {index} 的父线索引 {father} 超出范围。");
        }

        var state = new byte[judgeLines.Count];
        for (var start = 0; start < judgeLines.Count; start++)
        {
            var current = start;
            while (current >= 0 && state[current] == 0)
            {
                state[current] = 1;
                current = judgeLines[current].Father;
            }

            if (current >= 0 && state[current] == 1)
                throw new FormatException($"判定线父子关系包含循环，循环起点为 {current}。");

            current = start;
            while (current >= 0 && state[current] == 1)
            {
                state[current] = 2;
                current = judgeLines[current].Father;
            }
        }
    }

    /// <summary>
    /// 校验渲染配置、索引和位图尺寸。
    /// </summary>
    /// <param name="chart">待渲染谱面。</param>
    /// <param name="options">渲染配置。</param>
    /// <param name="lineIndex">可选判定线索引。</param>
    /// <param name="layerIndex">可选事件层索引。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateRender(
        Chart chart,
        KpcRenderOptions options,
        int? lineIndex = null,
        int? layerIndex = null
    )
    {
        ArgumentNullException.ThrowIfNull(chart);
        ValidateJudgeLineHierarchy(chart.JudgeLineList);
        ValidateRenderOptions(options);

#pragma warning disable CA2208
        if (
            !double.IsFinite(options.PixelsPerBeat)
            || options.PixelsPerBeat <= 0
            || options.PixelsPerBeat > 10_000
        )
            throw new ArgumentOutOfRangeException(nameof(options.PixelsPerBeat));
        if (options.ChannelWidth is <= 0 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(options.ChannelWidth));
        if (options.SamplesPerEvent is <= 0 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(options.SamplesPerEvent));
        if (options.BeatSubdivisions is <= 0 or > 128)
            throw new ArgumentOutOfRangeException(nameof(options.BeatSubdivisions));
#pragma warning restore CA2208
        if (
            options.LeftMargin < 0
            || options.HeaderHeight < 0
            || options.BottomPadding < 0
            || options.ChannelPadding < 0
            || options.StrokeWidth < 0
        )
            throw new ArgumentOutOfRangeException(nameof(options));
#pragma warning disable CA2208
        if (!double.IsFinite(options.RangePaddingRatio) || options.RangePaddingRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(options.RangePaddingRatio));
        if (options.RangeSamplesPerEvent is <= 0 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(options.RangeSamplesPerEvent));
        if (!double.IsFinite(options.SegmentGroupTolerance) || options.SegmentGroupTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(options.SegmentGroupTolerance));
        if (!double.IsFinite(options.MinValueRangeHalf) || options.MinValueRangeHalf < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MinValueRangeHalf));
        if (!double.IsFinite(options.MinValueRangeHalfRatio) || options.MinValueRangeHalfRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MinValueRangeHalfRatio));
#pragma warning restore CA2208
        if (lineIndex is < 0 || lineIndex >= chart.JudgeLineList.Count)
            throw new ArgumentOutOfRangeException(nameof(lineIndex));

        if (layerIndex is not null && lineIndex is null)
            throw new ArgumentException(
                "指定事件层索引时必须同时指定判定线索引。",
                nameof(layerIndex)
            );

        if (lineIndex is not null && layerIndex is not null)
        {
            var layers = chart.JudgeLineList[lineIndex.Value].EventLayers;
            if (layerIndex < 0 || layerIndex >= layers.Count)
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
        }

        var totalBeats = 4d;
        foreach (var layer in chart.JudgeLineList.SelectMany(line => line.EventLayers))
        {
            UpdateMax(layer.MoveXEvents?.Select(e => (double)e.EndBeat));
            UpdateMax(layer.MoveYEvents?.Select(e => (double)e.EndBeat));
            UpdateMax(layer.RotateEvents?.Select(e => (double)e.EndBeat));
            UpdateMax(layer.AlphaEvents?.Select(e => (double)e.EndBeat));
            UpdateMax(layer.SpeedEvents?.Select(e => (double)e.EndBeat));
        }

        if (totalBeats is < 0 or > 1_000_000d)
            throw new ArgumentOutOfRangeException(nameof(chart), "谱面总拍数超过安全上限。");

        var heightDouble =
            options.HeaderHeight
            + Math.Ceiling(totalBeats * options.PixelsPerBeat)
            + options.BottomPadding;
        if (!double.IsFinite(heightDouble) || heightDouble > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(options), "渲染位图高度超过安全上限。");
        var height = (long)heightDouble;
        var width =
            options.LeftMargin + 5L * options.ChannelWidth + 4L * options.ChannelPadding + 8L;
        if (
            height <= 0
            || width <= 0
            || width > int.MaxValue
            || width * height > MaximumRenderPixels
        )
            throw new ArgumentOutOfRangeException(nameof(options), "渲染位图尺寸超过安全上限。");
        return;

        void UpdateMax(IEnumerable<double>? values)
        {
            if (values is null)
                return;
            foreach (var value in values)
            {
                if (!double.IsFinite(value))
                    throw new FormatException("谱面事件拍数必须是有限数值。");
                if (value > totalBeats)
                    totalBeats = value;
            }
        }
    }

    /// <summary>
    /// 校验渲染器本身使用的配置，不依赖具体谱面。
    /// </summary>
    /// <param name="options">渲染配置。</param>
    /// <returns>无返回值。</returns>
    public static void ValidateRenderOptions(KpcRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
#pragma warning disable CA2208
        if (
            !double.IsFinite(options.PixelsPerBeat)
            || options.PixelsPerBeat <= 0
            || options.PixelsPerBeat > 10_000
        )
            throw new ArgumentOutOfRangeException(nameof(options.PixelsPerBeat));
        if (options.ChannelWidth is <= 0 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(options.ChannelWidth));
        if (options.SamplesPerEvent is <= 0 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(options.SamplesPerEvent));
        if (options.BeatSubdivisions is <= 0 or > 128)
            throw new ArgumentOutOfRangeException(nameof(options.BeatSubdivisions));
#pragma warning restore CA2208
        if (
            options.LeftMargin < 0
            || options.HeaderHeight < 0
            || options.BottomPadding < 0
            || options.ChannelPadding < 0
            || options.StrokeWidth < 0
        )
            throw new ArgumentOutOfRangeException(nameof(options));
#pragma warning disable CA2208
        if (!double.IsFinite(options.RangePaddingRatio) || options.RangePaddingRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(options.RangePaddingRatio));
        if (options.RangeSamplesPerEvent is <= 0 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(options.RangeSamplesPerEvent));
        if (!double.IsFinite(options.SegmentGroupTolerance) || options.SegmentGroupTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(options.SegmentGroupTolerance));
        if (!double.IsFinite(options.MinValueRangeHalf) || options.MinValueRangeHalf < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MinValueRangeHalf));
        if (!double.IsFinite(options.MinValueRangeHalfRatio) || options.MinValueRangeHalfRatio < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MinValueRangeHalfRatio));
#pragma warning restore CA2208
    }
}
