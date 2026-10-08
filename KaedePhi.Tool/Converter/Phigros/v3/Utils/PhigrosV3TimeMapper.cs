using KaedePhi.Core.Primitives;
using IrBpmItem = KaedePhi.Core.Intermediate.Model.BpmItem;

namespace KaedePhi.Tool.Converter.Phigros.v3.Utils;

internal sealed class PhigrosV3TimeMapper
{
    internal const float TargetBpm = 1000f;
    internal const float TailEventEndTime = 1_000_000_000f;

    private const long TailEventTime = 1_000_000_000L;
    private const double SecondsPerTimeUnit = 1.875d / TargetBpm;
    private const double MaximumTimeErrorSeconds = 0.001d;
    private readonly List<TempoSegment> _segments;

    public PhigrosV3TimeMapper(IReadOnlyList<IrBpmItem> bpmList)
    {
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
            else if (beat > new Beat(0) && Math.Abs(bpm - currentBpm) > Common.Constants.FloatEpsilon)
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
            seconds += (beat - segmentBeat) * 60d / segmentBpm;
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

    public double ToMusicTime(Beat beat) => GetSeconds(beat, 1f);

    public Beat ToBeat(double musicTime)
    {
        if (!double.IsFinite(musicTime))
            throw new FormatException("噪域音乐时间必须是有限数值。");

        var segment = _segments[0];
        for (var index = 1; index < _segments.Count; index++)
        {
            if (_segments[index].StartSeconds > musicTime)
                break;
            segment = _segments[index];
        }

        var beat =
            segment.StartBeat
            + (musicTime - segment.StartSeconds) * segment.Bpm / 60d;
        return !double.IsFinite(beat)
            ? throw new FormatException("噪域音乐时间转换后的拍数不是有限数值。")
            : new Beat(beat);
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
        return _segments.Skip(1).Where(segment => segment.StartBeat > startBeat)
            .TakeWhile(segment => segment.StartBeat < endBeat).Select(segment => segment.StartBeat);
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

        var segment = FindTempoSegment(beat);
        var seconds =
            (segment.StartSeconds + (beat - segment.StartBeat) * 60d / segment.Bpm)
            * bpmFactor;
        return !double.IsFinite(seconds) ? throw new FormatException("IR BPM 时间积分结果不是有限数值。") : seconds;
    }

    private TempoSegment FindTempoSegment(Beat beat)
    {
        if (_segments.Count == 1)
            return _segments[0];

        var lower = 0;
        var upper = _segments.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) >> 1);
            if (_segments[middle].StartBeat <= beat)
                lower = middle + 1;
            else
                upper = middle;
        }

        return _segments[Math.Max(0, lower - 1)];
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

    private readonly record struct TempoEntry(IrBpmItem Item, int Index);

    private readonly record struct TempoSegment(Beat StartBeat, float Bpm, double StartSeconds);
}