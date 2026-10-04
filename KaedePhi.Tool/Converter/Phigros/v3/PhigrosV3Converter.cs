using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.Phigros.v3.Model;
using KaedePhi.Tool.Converter.Phigros.v3.Utils;
using IrMeta = KaedePhi.Core.Intermediate.Model.Meta;
using PhigrosChart = KaedePhi.Core.Formats.Phigros.v3.Model.Chart;
using PhigrosJudgeLine = KaedePhi.Core.Formats.Phigros.v3.Model.JudgeLine;

namespace KaedePhi.Tool.Converter.Phigros.v3;

/// <summary>
/// Phigros V3 格式转换器。
/// </summary>
public class PhigrosV3Converter
    : LoggableBase,
        IChartConverter<PhigrosChart, Unit?, IrToPhigrosV3ConvertOptions>,
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
    /// 将 Phigros V3 格式转换为 IR 内部格式。
    /// </summary>
    /// <param name="input">Phigros V3 谱面</param>
    /// <param name="options">输入转换选项（未使用）</param>
    /// <returns>IR 谱面</returns>
    public Ir.Chart ToIr(PhigrosChart input, Unit? options)
    {
        ArgumentNullException.ThrowIfNull(input);

        _ct.ThrowIfCancellationRequested();

        var defaultBpm =
            input.JudgeLineList.Count > 0 ? input.JudgeLineList[0].Bpm : DefaultPhigrosBpm;

        var judgeLines = new List<Ir.JudgeLine>(input.JudgeLineList.Count);
        for (var i = 0; i < input.JudgeLineList.Count; i++)
        {
            _ct.ThrowIfCancellationRequested();
            judgeLines.Add(
                IrJudgeLineBuilder.ConvertJudgeLine(input.JudgeLineList[i], i, defaultBpm)
            );
        }

        var bpmList = BpmItemBuilder.ConvertBpmList(input.JudgeLineList);
        var timeMapper = new PhigrosV3TimeMapper(bpmList);
        var converted = new Ir.Chart
        {
            BpmList = bpmList,
            Meta = MetaBuilder.ConvertMeta(input),
            JudgeLineList = judgeLines,
            BlockAreaList = IrBlockAreaBuilder.ConvertBlockAreas(
                input.BlockAreaList,
                timeMapper,
                _ct
            ),
        };
        return IrChartNormalizer.NormalizeAndValidateNoteEndBeats(converted);
    }

    /// <summary>
    /// 将 IR 内部格式转换为 Phigros V3 格式。
    /// </summary>
    /// <param name="input">IR 谱面</param>
    /// <param name="options">输出转换选项</param>
    /// <returns>Phigros V3 谱面</returns>
    public PhigrosChart FromIr(Ir.Chart input, IrToPhigrosV3ConvertOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        ConversionOptionsValidator.Validate(options);
        var normalized = IrChartNormalizer.NormalizeAndValidateNoteEndBeats(input);
        IrChartValidator.ValidateJudgeLineHierarchy(normalized.JudgeLineList);
        _ct.ThrowIfCancellationRequested();

        WarnIfUnsupportedMeta(normalized.Meta);

        var hasBpmList = normalized.BpmList is { Count: > 0 };
        var timeMapper = hasBpmList ? new PhigrosV3TimeMapper(normalized.BpmList) : null;
        PhigrosV3JudgeLineBuilder judgeLineConverter;
        if (timeMapper is not null)
        {
            judgeLineConverter = new PhigrosV3JudgeLineBuilder(
                options,
                timeMapper,
                CalculateChartEndBeat(normalized),
                OnWarning
            );
        }
        else
        {
            judgeLineConverter = new PhigrosV3JudgeLineBuilder(
                options,
                options.DefaultBpm,
                CalculateChartEndTime(normalized),
                OnWarning
            );
        }

        var judgeLines = new List<PhigrosJudgeLine>(normalized.JudgeLineList.Count);
        foreach (var line in normalized.JudgeLineList)
        {
            _ct.ThrowIfCancellationRequested();
            var converted = judgeLineConverter.ConvertJudgeLine(line, normalized.JudgeLineList);
            if (converted is not null)
                judgeLines.Add(converted);
        }

        var blockAreaTimeMapper =
            timeMapper
            ?? new PhigrosV3TimeMapper(
                [new Ir.BpmItem { Bpm = options.DefaultBpm, StartBeat = new Beat(0) }]
            );
        var blockAreas = PhigrosV3BlockAreaBuilder.ConvertBlockAreas(
            normalized.BlockAreaList,
            blockAreaTimeMapper,
            options.Cutting.EasingPrecision,
            OnWarning,
            _ct
        );

        return new PhigrosChart
        {
            Offset = GetPhigrosV3Offset(normalized.Meta),
            JudgeLineList = judgeLines,
            BlockAreaList = blockAreas,
        };
    }

    private static float CalculateChartEndTime(Ir.Chart input)
    {
        return (float)(GetMaximumChartBeat(input) * 32d) + 1f;
    }

    private static Beat CalculateChartEndBeat(Ir.Chart input)
    {
        return new Beat(GetMaximumChartBeat(input) + 1d / 32d);
    }

    private static double GetMaximumChartBeat(Ir.Chart input)
    {
        var maxBeat = 0d;

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

        foreach (var blockArea in input.BlockAreaList)
        {
            maxBeat = Math.Max(maxBeat, (double)blockArea.AppearBeat);
            maxBeat = Math.Max(maxBeat, (double)blockArea.EnableBeat);
            maxBeat = Math.Max(maxBeat, (double)blockArea.DisableBeat);
            maxBeat = Math.Max(maxBeat, (double)blockArea.DisappearBeat);
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.MoveXEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.MoveYEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.RotateEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.RotateAnchorXEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.RotateAnchorYEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.ScaleXEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.ScaleYEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.ScaleAnchorXEvents));
            maxBeat = Math.Max(maxBeat, GetMaxEventEndBeat(blockArea.ScaleAnchorYEvents));
        }

        return maxBeat;
    }

    private static double GetMaxEventEndBeat<T>(List<IrEvents.Event<T>>? events)
        where T : notnull
    {
        if (events is not { Count: > 0 })
            return 0;
        return events.Max(e => (double)e.EndBeat);
    }

    private static float GetPhigrosV3Offset(IrMeta meta) => meta.Offset / 1000f;

    private void WarnIfUnsupportedMeta(IrMeta src) => WarnIfUnsupportedMeta("PhigrosV3", src);

    private void Warn(string message) => LogWarning(message);
}
