using KaedePhi.Core.Common;
using KpcBpmItem = KaedePhi.Core.KaedePhi.BpmItem;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

internal sealed class PhigrosV3TimeMapper
{
    internal const float TargetBpm = 1000f;
    internal const float TailEventEndTime = 1_000_000_000f;

    private const long TailEventTime = 1_000_000_000L;
    private const double SecondsPerTimeUnit = 1.875d / TargetBpm;
    private const double MaximumTimeErrorSeconds = 0.001d;
    private readonly List<TempoSegment> _segments;

    public PhigrosV3TimeMapper(IReadOnlyList<KpcBpmItem> bpmList)
    {
        if (bpmList is not { Count: > 0 })
            throw new ArgumentException("BPM 列表不能为空。", nameof(bpmList));

        if (bpmList.Any(item => item is null))
            throw new FormatException("谱面 BPM 列表包含空节点。");

        var ordered = bpmList
            .Select((item, index) => new TempoEntry(item, index))
            .OrderBy(entry => entry.Item.StartBeat)
            .ThenBy(entry => entry.Index)
            .ToList();

        var initialBpm = ordered[0].Item.Bpm;
        var changes = new List<(Beat Beat, float Bpm)>();
        var currentBpm = initialBpm;

        for (var start = 0; start < ordered.Count;)
        {
            var beat = ordered[start].Item.StartBeat;
            var end = start + 1;
            while (end < ordered.Count && ordered[end].Item.StartBeat == beat)
                end++;

            var bpm = ordered[end - 1].Item.Bpm;
            if (beat == new Beat(0))
            {
                initialBpm = bpm;
                currentBpm = bpm;
            }
            else if (beat > new Beat(0) && bpm != currentBpm)
            {
                changes.Add((beat, bpm));
                currentBpm = bpm;
            }

            start = end;
        }

        _segments = [new TempoSegment(new Beat(0), initialBpm, 0d)];
        if (changes.Count == 0)
            return;

        var segmentBpm = initialBpm;
        var segmentBeat = new Beat(0);
        var seconds = 0d;
        foreach (var (beat, bpm) in changes)
        {
            seconds += ((double)(beat - segmentBeat)) * 60d / segmentBpm;
            _segments.Add(new TempoSegment(beat, bpm, seconds));
            segmentBeat = beat;
            segmentBpm = bpm;
        }
    }

    public int ToNoteTime(Beat beat, float bpmFactor)
    {
        var (time, seconds) = Quantize(beat, bpmFactor);
        ValidateTailEventBoundary(time);
        if (time is < int.MinValue or > int.MaxValue)
            throw new FormatException("Phigros 音符时间超出可编码范围。");

        ValidateEncodedTime(time, seconds);
        return (int)time;
    }

    public float ToEventTime(Beat beat, float bpmFactor)
    {
        var (time, seconds) = Quantize(beat, bpmFactor);
        ValidateTailEventBoundary(time);
        var encoded = (float)time;
        if (encoded >= TailEventEndTime)
            throw new FormatException("Phigros 映射时间与尾事件哨兵冲突。");
        ValidateEncodedTime(encoded, seconds);
        return encoded;
    }

    public float ToHoldTime(Beat startBeat, Beat endBeat, float bpmFactor)
    {
        var startTime = ToNoteTime(startBeat, bpmFactor);
        var endTime = ToNoteTime(endBeat, bpmFactor);
        var holdTime = (float)((long)endTime - startTime);
        if (!float.IsFinite(holdTime) || holdTime <= 0f)
            throw new FormatException("Phigros Hold 音符映射后的持续时间必须大于零。");

        var endSeconds = GetSeconds(endBeat, bpmFactor);
        ValidateEncodedTime(startTime + holdTime, endSeconds);
        return holdTime;
    }

    public IEnumerable<Beat> GetTempoChangeBeats(Beat startBeat, Beat endBeat)
    {
        foreach (var segment in _segments.Skip(1))
        {
            if (segment.StartBeat <= startBeat)
                continue;
            if (segment.StartBeat >= endBeat)
                yield break;
            yield return segment.StartBeat;
        }
    }

    private (long Time, double Seconds) Quantize(Beat beat, float bpmFactor)
    {
        var seconds = GetSeconds(beat, bpmFactor);
        var time = seconds / SecondsPerTimeUnit;
        if (!double.IsFinite(time) || time is < long.MinValue or > long.MaxValue)
            throw new FormatException("Phigros 时间超出可编码范围。");

        var rounded = Math.Round(time, MidpointRounding.AwayFromZero);
        if (rounded is < long.MinValue or > long.MaxValue)
            throw new FormatException("Phigros 时间超出可编码范围。");

        return ((long)rounded, seconds);
    }

    private double GetSeconds(Beat beat, float bpmFactor)
    {
        if (!float.IsFinite(bpmFactor) || bpmFactor <= 0f)
            throw new FormatException("判定线 BPM 因子必须是有限正数。");

        var segment = _segments[0];
        for (var index = 1; index < _segments.Count; index++)
        {
            if (_segments[index].StartBeat > beat)
                break;
            segment = _segments[index];
        }

        var seconds =
            (segment.StartSeconds + ((double)(beat - segment.StartBeat)) * 60d / segment.Bpm)
            * bpmFactor;
        if (!double.IsFinite(seconds))
            throw new FormatException("KPC BPM 时间积分结果不是有限数值。");
        return seconds;
    }

    private static void ValidateEncodedTime(double encodedTime, double expectedSeconds)
    {
        if (!double.IsFinite(encodedTime))
            throw new FormatException("Phigros 时间超出可编码范围。");
        if (Math.Abs(encodedTime * SecondsPerTimeUnit - expectedSeconds) > MaximumTimeErrorSeconds)
            throw new FormatException("Phigros 时间量化误差超过 1 毫秒。");
    }

    private static void ValidateTailEventBoundary(long time)
    {
        if (time >= TailEventTime)
            throw new FormatException("Phigros 映射时间与尾事件哨兵冲突。");
    }

    private readonly record struct TempoEntry(KpcBpmItem Item, int Index);

    private readonly record struct TempoSegment(Beat StartBeat, float Bpm, double StartSeconds);
}
