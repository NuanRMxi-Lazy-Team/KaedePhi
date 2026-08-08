using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using KpcMeta = KaedePhi.Core.KaedePhi.Meta;
using PhigrosChart = KaedePhi.Core.Phigros.v3.Chart;
using PhigrosJudgeLine = KaedePhi.Core.Phigros.v3.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3;

/// <summary>
/// Phigros V3 格式转换器。
/// </summary>
public class PhigrosV3Converter
    : LoggableBase,
        IChartConverter<PhigrosChart, Unit?, KpcToPhigrosV3ConvertOptions>,
        ICancellableChartConverter
{
    /// <summary>
    /// Phigros 格式默认 BPM（当谱面未提供 BPM 时使用）。
    /// </summary>
    private const float DefaultPhigrosBpm = 120f;

    private CancellationToken _ct;

    /// <summary>
    /// 设置取消令牌。
    /// </summary>
    public void SetCancellationToken(CancellationToken ct) => _ct = ct;

    /// <summary>
    /// 将 Phigros V3 格式转换为 KPC 内部格式。
    /// </summary>
    /// <param name="input">Phigros V3 谱面</param>
    /// <param name="options">输入转换选项（未使用）</param>
    /// <returns>KPC 谱面</returns>
    public Kpc.Chart ToKpc(PhigrosChart input, Unit? options)
    {
        ArgumentNullException.ThrowIfNull(input);

        _ct.ThrowIfCancellationRequested();

        var defaultBpm =
            input.JudgeLineList.Count > 0 ? input.JudgeLineList[0].Bpm : DefaultPhigrosBpm;

        var judgeLines = new List<Kpc.JudgeLine>(input.JudgeLineList.Count);
        for (var i = 0; i < input.JudgeLineList.Count; i++)
        {
            _ct.ThrowIfCancellationRequested();
            judgeLines.Add(
                KpcJudgeLineBuilder.ConvertJudgeLine(input.JudgeLineList[i], i, defaultBpm)
            );
        }

        return new Kpc.Chart
        {
            BpmList = BpmItemBuilder.ConvertBpmList(input.JudgeLineList),
            Meta = MetaBuilder.ConvertMeta(input),
            JudgeLineList = judgeLines,
        };
    }

    /// <summary>
    /// 将 KPC 内部格式转换为 Phigros V3 格式。
    /// </summary>
    /// <param name="input">KPC 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>Phigros V3 谱面</returns>
    public PhigrosChart FromKpc(Kpc.Chart input, KpcToPhigrosV3ConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        _ct.ThrowIfCancellationRequested();

        WarnIfUnsupportedMeta(input.Meta);

        var globalBpm = input.BpmList is { Count: > 0 } ? input.BpmList[0].Bpm : options.DefaultBpm;
        var chartEndBeat = CalculateChartEndBeat(input);

        var judgeLineConverter = new PhigrosV3JudgeLineBuilder(
            options,
            globalBpm,
            chartEndBeat,
            OnWarning
        );

        var judgeLines = new List<PhigrosJudgeLine>(input.JudgeLineList.Count);
        foreach (var line in input.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            judgeLines.Add(judgeLineConverter.ConvertJudgeLine(line, input.JudgeLineList));
        }

        return new PhigrosChart
        {
            Offset = GetPhigrosV3Offset(input.Meta),
            JudgeLineList = judgeLines,
        };
    }

    private static float CalculateChartEndBeat(Kpc.Chart input)
    {
        var maxBeat = 0d;

        if (input.JudgeLineList is not { Count: > 0 })
            return (float)(maxBeat * 32) + 1f;
        foreach (var line in input.JudgeLineList)
        {
            if (line.Notes is { Count: > 0 })
                maxBeat = line.Notes.Select(note => (double)note.EndBeat).Prepend(maxBeat).Max();

            if (line.EventLayers is not { Count: > 0 })
                continue;
            foreach (var layer in line.EventLayers)
            {
                maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(layer.MoveXEvents));
                maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(layer.MoveYEvents));
                maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(layer.RotateEvents));
                maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(layer.AlphaEvents));
                maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(layer.SpeedEvents));
            }
        }

        return (float)(maxBeat * 32) + 1f;
    }

    private static double GetMaxEventEndBeat<T>(List<KpcEvents.Event<T>>? events)
        where T : notnull
    {
        if (events is not { Count: > 0 })
            return 0;
        return events.Max(e => (double)e.EndBeat);
    }

    private static float GetPhigrosV3Offset(KpcMeta meta) => meta.Offset / 1000f;

    private void WarnIfUnsupportedMeta(KpcMeta src) => WarnIfUnsupportedMeta("PhigrosV3", src);

    private void Warn(string message) => LogWarning(message);
}
