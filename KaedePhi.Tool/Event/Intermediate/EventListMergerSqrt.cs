using KaedePhi.Core.Primitives;

namespace KaedePhi.Tool.Event.Intermediate;

/// <summary>
/// 采用欧几里得垂直距离（Sqrt 路线）判定自适应分段切割的事件合并器。
/// <para>
/// 比例尺取 <b>两个子段各自 swing 的最大值</b>：
/// <c>scale = max(|nextNum−startNum|, |endNum−nextNum|, 1e-3)</c>，
/// 与 <see cref="EventCompressor{TPayload}.EventListCompressSqrt"/> 的 <c>TryMergeSqrt</c> 逻辑完全对齐。
/// </para>
/// <para>
/// 有效阈值：<c>|deviation| / (scale × sqrt(1 + dvNorm²)) &lt; tolerance%</c>。
/// 相比基类（以剩余 swing 为比例尺），本类使用局部子段尺度，
/// 对早期小振幅阶段更灵敏，适合幅度随时间增长的缓动曲线。
/// </para>
/// </summary>
/// <typeparam name="TPayload">事件值类型（<see langword="int"/>、<see langword="float"/> 或 <see langword="double"/>）。</typeparam>
public class EventListMergerSqrt<TPayload> : EventListMergerPlus<TPayload>
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

        var p = Math.Clamp(dtLocal / dtTotal, 0.0, 1.0);

        var startNum = ToDouble(segmentStartSum);
        var nextNum = ToDouble(sumAtNext);
        var endNum = ToDouble(sumAtEnd);

        // 以两个子段各自 swing 的最大值为归一化尺度，与 EventCompressor.TryMergeSqrt 对齐：
        //   rangeFirst = |B − A|（segmentStart → nextBeat）
        //   rangeSecond = |C − B|（nextBeat → intervalEnd）
        //   scale = max(rangeFirst, rangeSecond, 1e-3)
        // 相比基类（scale = 剩余总 swing），本方案对早期小振幅段更灵敏。
        var rangeFirst = Math.Abs(nextNum - startNum);
        var rangeSecond = Math.Abs(endNum - nextNum);
        var scale = Math.Max(Math.Max(rangeFirst, rangeSecond), 1e-3);

        // 与 TryMergeSqrt 相同的欧氏归一化垂直距离公式
        var normalizedValueDelta = (endNum - startNum) / scale;
        var normalizedMidValue = (nextNum - startNum) / scale;
        var linearDeviation = normalizedMidValue - normalizedValueDelta * p;
        var mergedLineLength = Math.Sqrt(1.0 + normalizedValueDelta * normalizedValueDelta);

        return Math.Abs(linearDeviation) / mergedLineLength > Math.Max(0d, tolerance) / 100.0;
    }
}
