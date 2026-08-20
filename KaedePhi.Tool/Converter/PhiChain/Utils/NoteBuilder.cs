using KaedePhi.Core.Common;
using KaedePhi.Core.PhiChain.v6;
using KaedePhi.Core.Utils;
using KpcNoteType = KaedePhi.Core.Common.NoteType;
using PhiChainNoteType = KaedePhi.Core.PhiChain.v6.NoteType;

namespace KaedePhi.Tool.Converter.PhiChain.Utils;

/// <summary>
/// PhiChain 与 KPC 音符之间的双向转换工具。
/// </summary>
public static class NoteBuilder
{
    /// <summary>
    /// 将 PhiChain 音符转换为 KPC 音符。
    /// </summary>
    /// <param name="src">PhiChain 音符</param>
    /// <returns>KPC 音符</returns>
    public static Kpc.Note ConvertNote(Note src)
    {
        if (src.Type == PhiChainNoteType.Hold && src.HoldBeat <= new Beat(0))
            throw new FormatException("PhiChain Hold 音符缺少有效的持续拍。");

        var kpcNote = new Kpc.Note
        {
            Above = src.Above,
            StartBeat = new Beat((int[])src.Beat),
            EndBeat = new Beat((int[])src.Beat),
            PositionX = Transform.TransformToKpcX(src.X),
            SpeedMultiplier = src.Speed,
            Type = ConvertNoteType(src.Type),
        };

        // Hold 音符需要设置 EndBeat
        if (src.Type == PhiChainNoteType.Hold)
        {
            kpcNote.EndBeat = new Beat((int[])src.Beat) + new Beat((int[])src.HoldBeat);
        }

        return kpcNote;
    }

    /// <summary>
    /// 将 KPC 音符转换为 PhiChain 音符。
    /// </summary>
    /// <param name="src">KPC 音符</param>
    /// <returns>PhiChain 音符</returns>
    public static Note ConvertNote(Kpc.Note src)
    {
        var note = new Note
        {
            Above = src.Above,
            Beat = new Beat((int[])src.StartBeat),
            X = Transform.TransformToPhiChainX(src.PositionX),
            Speed = src.SpeedMultiplier,
            Type = ConvertNoteType(src.Type),
        };

        // Hold 音符需要设置 HoldBeat
        if (src.Type == KpcNoteType.Hold)
        {
            note.HoldBeat = new Beat((int[])(src.EndBeat - src.StartBeat));
        }

        return note;
    }

    /// <summary>
    /// 检查 KPC 音符字段是否会被 PhiChain 丢弃，发出警告。
    /// </summary>
    /// <param name="src">KPC 音符</param>
    /// <param name="warn">警告回调</param>
    public static void WarnIfUnsupportedNoteFields(Kpc.Note src, Action<string>? warn)
    {
        if (warn == null)
            return;

        var defaults = new Kpc.Note();
        if (src.Alpha != defaults.Alpha)
            warn($"PhiChain 不支持 Note.Alpha（值={src.Alpha}）");
        if (src.IsFake)
            warn($"PhiChain 不支持 Note.IsFake（值=true）");
        if (Math.Abs(src.WidthRatio - defaults.WidthRatio) > 0.0001f)
            warn($"PhiChain 不支持 Note.WidthRatio（值={src.WidthRatio}）");
        if (Math.Abs(src.JudgeArea - defaults.JudgeArea) > 0.0001f)
            warn($"PhiChain 不支持 Note.JudgeArea（值={src.JudgeArea}）");
        if (Math.Abs(src.VisibleTime - defaults.VisibleTime) > 0.0001f)
            warn($"PhiChain 不支持 Note.VisibleTime（值={src.VisibleTime}）");
        if (Math.Abs(src.YOffset - defaults.YOffset) > 0.0001)
            warn($"PhiChain 不支持 Note.YOffset（值={src.YOffset}）");
        if (
            src.Tint[0] != defaults.Tint[0]
            || src.Tint[1] != defaults.Tint[1]
            || src.Tint[2] != defaults.Tint[2]
        )
            warn($"PhiChain 不支持 Note.Tint（值=[{src.Tint[0]}, {src.Tint[1]}, {src.Tint[2]}]）");
        if (src.HitFxColor != null)
            warn($"PhiChain 不支持 Note.HitFxColor");
        if (src.HitSound != null)
            warn($"PhiChain 不支持 Note.HitSound（值='{src.HitSound}'）");
    }

    /// <summary>
    /// 将 CurveNoteTrack 展开为普通音符列表。
    /// </summary>
    /// <param name="track">曲线音符轨道</param>
    /// <param name="fromNote">起始音符</param>
    /// <param name="toNote">结束音符</param>
    /// <returns>展开后的音符列表</returns>
    public static List<Kpc.Note> ExpandCurveNoteTrack(
        CurveNoteTrack track,
        Note fromNote,
        Note toNote
    )
    {
        var notes = new List<Kpc.Note>();
        var density = track.Density;
        if (density == 0)
            density = 16;

        var startBeat = new Beat((int[])fromNote.Beat);
        var endBeat = new Beat((int[])toNote.Beat);

        var startBeatVal = (double)startBeat;
        var endBeatVal = (double)endBeat;
        var totalBeats = endBeatVal - startBeatVal;
        var noteType = ConvertNoteType(track.NoteType);
        if (
            noteType == KpcNoteType.Hold
            && (track.HoldBeat is null || track.HoldBeat <= new Beat(0))
        )
            throw new FormatException("PhiChain Hold 曲线音符缺少有效的持续拍。");
        if (totalBeats <= 0)
            return notes;

        var step = 1.0 / density;

        // 从 start 开始按步长生成，skip(1) 跳过起始音符
        var beat = startBeatVal + step;
        var i = 1;
        while (beat < endBeatVal)
        {
            var t = (beat - startBeatVal) / totalBeats;
            var easedT = ApplyCurve(t, track.Curve);

            var x = fromNote.X + (toNote.X - fromNote.X) * (float)easedT;
            var noteBeat = new Beat(beat);

            var note = new Kpc.Note
            {
                Above = fromNote.Above,
                StartBeat = noteBeat,
                EndBeat = new Beat((int[])noteBeat),
                PositionX = Transform.TransformToKpcX(x),
                SpeedMultiplier = fromNote.Speed,
                Type = noteType,
            };

            if (noteType == KpcNoteType.Hold)
            {
                note.EndBeat = noteBeat + new Beat((int[])track.HoldBeat!);
            }

            notes.Add(note);

            i++;
            beat = startBeatVal + i * step;
        }

        return notes;
    }

    /// <summary>
    /// 应用曲线缓动函数。
    /// </summary>
    private static double ApplyCurve(double t, Easing curve)
    {
        return curve.EasingType switch
        {
            // 线性直接返回
            EasingKind.Linear => t,
            // Steps 缓动：阶梯式量化
            EasingKind.Steps => curve.Count > 0 ? Math.Round(t * curve.Count) / curve.Count : t,
            // Elastic 缓动：弹性振荡
            EasingKind.Elastic => ApplyElastic(t, curve.Omega),
            // 自定义贝塞尔曲线
            EasingKind.Custom => Bezier.Do(
                [curve.X1, curve.Y1, curve.X2, curve.Y2],
                (float)t,
                0d,
                1d
            ),
            // 其他标准缓动
            _ => ApplyStandardEasing(t, curve),
        };
    }

    /// <summary>
    /// 应用弹性缓动函数
    /// </summary>
    private static double ApplyElastic(double t, float omega)
    {
        if (Math.Abs(omega) <= Common.Constants.FloatEpsilon)
            return t;
        var omegad = (double)omega;
        return 1.0
            - Math.Pow(1.0 - t, 2) * (2.0 * Math.Sin(omegad * t) / omegad + Math.Cos(omegad * t));
    }

    /// <summary>
    /// 应用标准缓动函数。
    /// </summary>
    private static double ApplyStandardEasing(double t, Easing curve)
    {
        try
        {
            var easingNumber = EasingConverter.ConvertToKpcEasingNumber(curve);
            var kpcEasing = new Kpc.Easing(easingNumber);
            return kpcEasing.Interpolate(0f, 1f, 0.0, 1.0, t);
        }
        catch (EasingConverter.EasingNotSupportedException)
        {
            // 不支持的缓动类型回退到线性
            return t;
        }
    }

    /// <summary>
    /// 转换音符类型。
    /// </summary>
    private static KpcNoteType ConvertNoteType(PhiChainNoteType src)
    {
        return src switch
        {
            PhiChainNoteType.Tap => KpcNoteType.Tap,
            PhiChainNoteType.Drag => KpcNoteType.Drag,
            PhiChainNoteType.Hold => KpcNoteType.Hold,
            PhiChainNoteType.Flick => KpcNoteType.Flick,
            _ => KpcNoteType.Tap,
        };
    }

    /// <summary>
    /// 转换音符类型（反向）。
    /// </summary>
    private static PhiChainNoteType ConvertNoteType(KpcNoteType src)
    {
        return src switch
        {
            KpcNoteType.Tap => PhiChainNoteType.Tap,
            KpcNoteType.Drag => PhiChainNoteType.Drag,
            KpcNoteType.Hold => PhiChainNoteType.Hold,
            KpcNoteType.Flick => PhiChainNoteType.Flick,
            _ => PhiChainNoteType.Tap,
        };
    }
}
