using KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Event.Intermediate;

/// <summary>
/// 采用归一化斜率差（Slope 路线）判定自适应分段切割的事件合并器。
/// <para>
/// 比例尺取 <b>两个子段各自 swing 的最大值</b>：
/// <c>scale = max(|nextNum−startNum|, |endNum−nextNum|, 1e-3)</c>，
/// 与 <see cref="EventCompressor{TPayload}.EventListCompressSlope"/> 的 <c>TryMergeSlope</c> 逻辑完全对齐。
/// </para>
/// <para>
/// 判定条件：<c>|firstSlope − secondSlope| &gt; tolerance%</c>，其中斜率以 scale 归一化。
/// 相较欧氏垂直距离，斜率差对曲线在测试点附近的 <b>速度突变</b> 更灵敏，
/// 适合检测缓入缓出曲线在零速区域的细微弯曲。
/// </para>
/// </summary>
/// <typeparam name="TPayload">事件值类型（<see langword="int"/>、<see langword="float"/> 或 <see langword="double"/>）。</typeparam>
public class EventListMergerSlope<TPayload> : EventListMergerPlus<TPayload>
    where TPayload : notnull
{
    /// <inheritdoc/>
    protected override bool ShouldSplitAdaptiveSegment(
        Beat segmentStart,
        Beat nextBeat,
        Beat intervalEnd,
        TPayload? segmentStartSum,
        TPayload? sumAtNext,
        TPayload? sumAtEnd,
        double tolerance
    )
    {
        if (nextBeat >= intervalEnd)
            return true;
        if (nextBeat <= segmentStart)
            return false;

        var dtTotal = (double)(intervalEnd - segmentStart);
        var dtLocal = (double)(nextBeat - segmentStart);
        if (dtTotal <= 1e-12 || dtLocal <= 1e-12)
            return false;

        var startNum = ToDouble(segmentStartSum);
        var nextNum = ToDouble(sumAtNext);
        var endNum = ToDouble(sumAtEnd);

        // 以两个子段各自 swing 的最大值为归一化尺度，与 EventCompressor.TryMergeSlope 对齐：
        //   rangeFirst = |B − A|（segmentStart → nextBeat）
        //   rangeSecond = |C − B|（nextBeat → intervalEnd）
        //   scale = max(rangeFirst, rangeSecond, 1e-3)
        var rangeFirst = Math.Abs(nextNum - startNum);
        var rangeSecond = Math.Abs(endNum - nextNum);
        var scale = Math.Max(Math.Max(rangeFirst, rangeSecond), 1e-3);

        // 与 TryMergeSlope 相同的归一化斜率差公式：
        //   firstSlope  = (B − A) / dtLocal     / scale  （前半段斜率）
        //   secondSlope = (C − B) / dtRemaining / scale  （后半段斜率）
        //   split if |firstSlope − secondSlope| > tolerance%
        var remainingTime = dtTotal - dtLocal;
        var firstSlope = dtLocal < 1e-12 ? 0.0 : (nextNum - startNum) / dtLocal / scale;
        var secondSlope = remainingTime < 1e-12 ? 0.0 : (endNum - nextNum) / remainingTime / scale;

        return Math.Abs(firstSlope - secondSlope) > Math.Max(0d, tolerance) / 100.0;
    }
}
