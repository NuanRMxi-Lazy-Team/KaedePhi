using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KaedePhi.Core.Common;

namespace KaedePhi.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 解析谱面中的一行文本，将结果写入 <paramref name="chart"/> 或 <paramref name="judgeDict"/>。
        /// <para>
        /// 若当前行为 Note 指令且未内联速度/宽度信息，则通过 <paramref name="readNextLineFunc"/> 额外读取紧跟的两行。
        /// </para>
        /// </summary>
        /// <param name="line">当前非空白文本行。</param>
        /// <param name="readNextLineFunc">用于按需读取后续行（Note 多行格式）的函数。</param>
        /// <param name="chart">正在构建的谱面对象。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <exception cref="FormatException">指令字段数不足，或 Note 缺失速度/宽度行。</exception>
        private static void ParseChartLineCore(
            string line,
            Func<string?> readNextLineFunc,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            var part = SplitWhitespace(line);
            var judgeLineIndex = GetJudgeLineIndex(part);

            if (part[0] == "bp")
            {
                EnsureMinParts(part, 3, "bp");
                chart.BpmList.Add(
                    new BpmItem
                    {
                        StartBeat = ParseFloat(part[1], "bp 起始拍"),
                        Bpm = ParseFloat(part[2], "bp BPM"),
                    }
                );
            }
            else if (part[0].StartsWith('n'))
            {
                var (speedPart, widthPart) = GetInlineNoteParts(part);
                if (speedPart is null)
                {
                    var speedLine = readNextLineFunc();
                    var widthLine = readNextLineFunc();
                    if (speedLine is null || widthLine is null)
                        throw new FormatException("Malformed note: missing speed or width lines.");
                    speedPart = SplitWhitespace(speedLine);
                    widthPart = SplitWhitespace(widthLine);
                }

                AddNoteToDict(BuildNote(part, speedPart, widthPart), judgeLineIndex, judgeDict);
            }
            else
                ParseLineCommand(part, judgeLineIndex, judgeDict);
        }

        /// <summary>
        /// 解析谱面中的一行文本，将结果写入 <paramref name="chart"/> 或 <paramref name="judgeDict"/>。
        /// <para>
        /// 若当前行为 Note 指令且未内联速度/宽度信息，则从 <paramref name="lines"/> 额外读取紧跟的两行。
        /// </para>
        /// </summary>
        /// <param name="line">当前非空白文本行。</param>
        /// <param name="lines">谱面全部文本行数组。</param>
        /// <param name="index">当前行在 <paramref name="lines"/> 中的索引。</param>
        /// <param name="chart">正在构建的谱面对象。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <returns>消耗的行数（1 为仅当前行，3 为包含后续两行）。</returns>
        /// <exception cref="FormatException">指令字段数不足，或 Note 缺失速度/宽度行。</exception>
        private static int ParseChartLineCore(
            string line,
            string[] lines,
            int index,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            var part = SplitWhitespace(line);
            var judgeLineIndex = GetJudgeLineIndex(part);

            if (part[0] == "bp")
            {
                EnsureMinParts(part, 3, "bp");
                chart.BpmList.Add(
                    new BpmItem
                    {
                        StartBeat = ParseFloat(part[1], "bp 起始拍"),
                        Bpm = ParseFloat(part[2], "bp BPM"),
                    }
                );
            }
            else if (part[0].StartsWith('n'))
            {
                var (speedPart, widthPart) = GetInlineNoteParts(part);
                if (speedPart is null)
                {
                    if (index + 2 >= lines.Length)
                        throw new FormatException(
                            $"Malformed note at line {index + 1}: missing speed or width lines."
                        );
                    speedPart = SplitWhitespace(lines[index + 1]);
                    widthPart = SplitWhitespace(lines[index + 2]);
                    AddNoteToDict(BuildNote(part, speedPart, widthPart), judgeLineIndex, judgeDict);
                    return 3;
                }

                AddNoteToDict(BuildNote(part, speedPart, widthPart), judgeLineIndex, judgeDict);
            }
            else
                ParseLineCommand(part, judgeLineIndex, judgeDict);

            return 1;
        }

        /// <summary>
        /// 校验指令的字段数量是否满足最低要求；不满足时抛出包含命令名称和实际/期望字段数的 <see cref="FormatException"/>。
        /// </summary>
        /// <param name="part">已按空格拆分的指令字段数组。</param>
        /// <param name="min">该指令要求的最小字段数（含指令标识符本身）。</param>
        /// <param name="cmd">指令名称，用于生成错误消息（如 <c>"bp"</c>、<c>"cm"</c>）。</param>
        /// <exception cref="FormatException"><paramref name="part"/> 的长度小于 <paramref name="min"/>。</exception>
        private static void EnsureMinParts(string[] part, int min, string cmd)
        {
            if (part.Length < min)
                throw new FormatException(
                    $"Malformed '{cmd}' command: expected at least {min} parts, got {part.Length}."
                );
        }

        /// <summary>
        /// 将一行文本按空白字符拆分为字段数组，并移除空字段。
        /// </summary>
        /// <param name="line">要拆分的文本行。</param>
        /// <returns>拆分后的字段数组。</returns>
        private static string[] SplitWhitespace(string line) =>
            line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// 解析指令的判定线索引字段（<c>part[1]</c>），若为 <c>"bp"</c> 则返回 -1。
        /// </summary>
        /// <param name="part">已按空格拆分的指令字段数组。</param>
        /// <returns>判定线索引，若为 <c>"bp"</c> 则返回 -1。</returns>
        /// <exception cref="FormatException"><paramref name="part"/> 的长度不足或格式错误。</exception>
        private static int GetJudgeLineIndex(string[] part)
        {
            if (part.Length == 0)
                throw new FormatException("Malformed chart command: command is empty.");
            if (part[0] == "bp")
                return -1;

            EnsureMinParts(part, 2, part[0]);
            return ParseInteger(part[1], $"{part[0]} 判定线索引");
        }

        /// <summary>
        /// 尝试将文本解析为整数，使用不区分区域的整数格式。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="value">解析成功时的整数值。</param>
        /// <returns>如果解析成功，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
        private static bool TryParseInteger(string? text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// 尝试将文本解析为整数。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="field">字段名称，用于生成错误消息。</param>
        /// <returns>解析成功的整数值。</returns>
        /// <exception cref="FormatException">解析失败。</exception>
        private static int ParseInteger(string text, string field)
        {
            return !TryParseInteger(text, out var value)
                ? throw new FormatException($"Malformed chart field '{field}': '{text}'.")
                : value;
        }

        /// <summary>
        /// 尝试将文本解析为浮点数，使用不区分区域的浮点格式。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="field">字段名称，用于生成错误消息。</param>
        /// <returns>解析成功的浮点数值。</returns>
        /// <exception cref="FormatException">解析失败。</exception>
        private static float ParseFloat(string text, string field)
        {
            if (
                !float.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value
                )
                || float.IsNaN(value)
                || float.IsInfinity(value)
            )
                throw new FormatException($"Malformed chart field '{field}': '{text}'.");
            return value;
        }

        /// <summary>
        /// 尝试将文本解析为二进制标记（0/1），用于表示布尔值。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="field">字段名称，用于生成错误消息。</param>
        /// <returns>解析成功的布尔值。</returns>
        /// <exception cref="FormatException">解析失败。</exception>
        private static bool ParseBinaryFlag(string text, string field) =>
            text switch
            {
                "0" => false,
                "1" => true,
                _ => throw new FormatException($"Malformed chart field '{field}': '{text}'."),
            };

        /// <summary>
        /// 尝试将文本解析为上下侧标记（1/2），用于表示布尔值。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <returns>解析成功的布尔值。</returns>
        /// <exception cref="FormatException">解析失败。</exception>
        private static bool ParseAboveFlag(string text) =>
            text switch
            {
                "1" => true,
                "2" => false,
                _ => throw new FormatException("Malformed note field 'note 上下侧'."),
            };

        /// <summary>
        /// 根据指令类型（<c>cv</c>/<c>cp</c>/<c>cd</c>/<c>ca</c>/<c>cm</c>/<c>cr</c>/<c>cf</c>）
        /// 解析对应的关键帧或事件，追加到 <paramref name="judgeDict"/> 中对应判定线的集合内。
        /// <para>
        /// 未知指令类型将被静默忽略。若指令所对应的判定线尚不存在，会自动创建并注册。
        /// </para>
        /// </summary>
        /// <param name="part">已按空格拆分的指令字段数组，<c>part[0]</c> 为指令标识符，<c>part[1]</c> 为判定线索引。</param>
        /// <param name="judgeLineIndex">当前指令作用的判定线索。</param>
        /// <param name="judgeDict">判定线暂存字典，解析结果将就地写入。</param>
        /// <exception cref="FormatException">指令字段数不足。</exception>
        private static void ParseLineCommand(
            string[] part,
            int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            switch (part[0])
            {
                case "cv":
                    EnsureMinParts(part, 4, "cv");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .SpeedFrames.Add(
                            new Frame
                            {
                                Beat = ParseFloat(part[2], "cv 拍数"),
                                Value = ParseFloat(part[3], "cv 数值"),
                            }
                        );
                    break;
                case "cp":
                    EnsureMinParts(part, 5, "cp");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .MoveFrames.Add(
                            new MoveFrame
                            {
                                Beat = ParseFloat(part[2], "cp 拍数"),
                                XValue = ParseFloat(part[3], "cp X 数值"),
                                YValue = ParseFloat(part[4], "cp Y 数值"),
                            }
                        );
                    break;
                case "cd":
                    EnsureMinParts(part, 4, "cd");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .RotateFrames.Add(
                            new Frame
                            {
                                Beat = ParseFloat(part[2], "cd 拍数"),
                                Value = ParseFloat(part[3], "cd 数值"),
                            }
                        );
                    break;
                case "ca":
                    EnsureMinParts(part, 4, "ca");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .AlphaFrames.Add(
                            new Frame
                            {
                                Beat = ParseFloat(part[2], "ca 拍数"),
                                Value = ParseFloat(part[3], "ca 数值"),
                            }
                        );
                    break;
                case "cm":
                    EnsureMinParts(part, 7, "cm");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .MoveEvents.Add(
                            new MoveEvent
                            {
                                StartBeat = ParseFloat(part[2], "cm 起始拍"),
                                EndBeat = ParseFloat(part[3], "cm 结束拍"),
                                EndXValue = ParseFloat(part[4], "cm X 数值"),
                                EndYValue = ParseFloat(part[5], "cm Y 数值"),
                                EasingType = new Easing(ParseInteger(part[6], "cm 缓动类型")),
                            }
                        );
                    break;
                case "cr":
                    EnsureMinParts(part, 6, "cr");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .RotateEvents.Add(
                            new Event
                            {
                                StartBeat = ParseFloat(part[2], "cr 起始拍"),
                                EndBeat = ParseFloat(part[3], "cr 结束拍"),
                                EndValue = ParseFloat(part[4], "cr 数值"),
                                EasingType = new Easing(ParseInteger(part[5], "cr 缓动类型")),
                            }
                        );
                    break;
                case "cf":
                    EnsureMinParts(part, 5, "cf");
                    Ensure();
                    judgeDict[judgeLineIndex]
                        .AlphaEvents.Add(
                            new Event
                            {
                                StartBeat = ParseFloat(part[2], "cf 起始拍"),
                                EndBeat = ParseFloat(part[3], "cf 结束拍"),
                                EndValue = ParseFloat(part[4], "cf 数值"),
                                EasingType = Easing.Linear,
                            }
                        );
                    break;
            }

            return;

            void Ensure()
            {
                if (!judgeDict.ContainsKey(judgeLineIndex))
                    judgeDict[judgeLineIndex] = new JudgeLine();
            }
        }

        /// <summary>
        /// 根据已拆分的字段数组构造一个 <see cref="Note"/> 对象。
        /// <para>
        /// <c>part[0]</c> 的第二个字符决定音符类型；Hold 音符（类型 2）会从 <c>part</c> 读取结束拍，
        /// 其余音符的结束拍等于起始拍。速度倍率和宽度比例分别从
        /// <paramref name="noteSpeedMultiplierPart"/>[1] 和 <paramref name="noteWidthRatioPart"/>[1] 读取。
        /// </para>
        /// </summary>
        /// <param name="part">Note 主指令字段数组（至少 4 个元素）。</param>
        /// <param name="noteSpeedMultiplierPart">速度行字段数组，格式为 <c>["#", value]</c>（至少 2 个元素）。</param>
        /// <param name="noteWidthRatioPart">宽度行字段数组，格式为 <c>["&amp;", value]</c>（至少 2 个元素）。</param>
        /// <returns>完整填充的 <see cref="Note"/> 实例。</returns>
        /// <exception cref="FormatException">任意字段数组元素数量不足。</exception>
        private static Note BuildNote(
            string[] part,
            string[] noteSpeedMultiplierPart,
            string[]? noteWidthRatioPart
        )
        {
            if (noteWidthRatioPart is null)
                throw new FormatException("Malformed note: missing width ratio part.");
            if (noteSpeedMultiplierPart.Length < 2)
                throw new FormatException(
                    "Malformed note speed multiplier part: expected at least 2 elements."
                );
            if (noteWidthRatioPart.Length < 2)
                throw new FormatException(
                    "Malformed note width ratio part: expected at least 2 elements."
                );

            if (
                part[0].Length < 2
                || !int.TryParse(
                    part[0][1..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var noteTypeValue
                )
            )
                throw new FormatException("Malformed note command: invalid note type.");
            if (!Enum.IsDefined(typeof(NoteType), noteTypeValue))
                throw new FormatException("Malformed note command: unsupported note type.");

            var noteType = (NoteType)noteTypeValue;
            var isHold = noteType == NoteType.Hold;
            var requiredPartCount = isHold ? 7 : 6;
            var inlineMarkerIndex = Array.IndexOf(part, "#");
            var notePartCount = inlineMarkerIndex >= 0 ? inlineMarkerIndex : part.Length;
            if (notePartCount < requiredPartCount)
                throw new FormatException(
                    $"Malformed 'note' command: expected at least {requiredPartCount} parts, got {notePartCount}."
                );
            if (noteSpeedMultiplierPart[0] != "#" || noteWidthRatioPart[0] != "&")
                throw new FormatException("Malformed note: invalid speed or width marker.");

            var startBeat = ParseFloat(part[2], "note 起始拍");
            var endBeat = isHold ? ParseFloat(part[3], "note 结束拍") : startBeat;
            if (isHold && endBeat <= startBeat)
                throw new FormatException("Hold 音符的结束拍必须晚于开始拍。");

            return new Note
            {
                StartBeat = startBeat,
                EndBeat = endBeat,
                PositionX = ParseFloat(part[isHold ? 4 : 3], "note X 坐标"),
                Above = ParseAboveFlag(part[isHold ? 5 : 4]),
                IsFake = ParseBinaryFlag(part[isHold ? 6 : 5], "note 假音符标记"),
                SpeedMultiplier = ParseFloat(noteSpeedMultiplierPart[1], "note 速度倍率"),
                WidthRatio = ParseFloat(noteWidthRatioPart[1], "note 宽度比例"),
                Type = noteType,
            };
        }

        /// <summary>
        /// 从 Note 主指令字段数组中尝试提取内联的速度行和宽度行。
        /// <para>
        /// 部分不规范谱面允许将速度（<c># value</c>）和宽度（<c>&amp; value</c>）以空格连接内联在同一行中；
        /// 本方法通过查找 <c>#</c> 和 <c>&amp;</c> 标记判断是否为内联格式。
        /// </para>
        /// </summary>
        /// <param name="part">Note 行按空格拆分后的全部字段。</param>
        /// <returns>
        /// 若找到内联的速度和宽度信息，返回对应的两个字段数组元组 <c>(speedPart, widthPart)</c>；
        /// 否则两者均为 <see langword="null"/>，表示需要额外读取后续两行。
        /// </returns>
        private static (string[]? speedPart, string[]? widthPart) GetInlineNoteParts(string[] part)
        {
            var hashIndex = Array.IndexOf(part, "#");
            var ampIndex = Array.IndexOf(part, "&");
            if (
                hashIndex != -1
                && ampIndex != -1
                && hashIndex + 1 < part.Length
                && ampIndex + 1 < part.Length
            )
                return (new[] { "#", part[hashIndex + 1] }, new[] { "&", part[ampIndex + 1] });
            return (null, null);
        }

        /// <summary>
        /// 将 <paramref name="note"/> 追加到 <paramref name="judgeDict"/> 中对应判定线的 <see cref="JudgeLine.NoteList"/>。
        /// 若指定索引的判定线尚不存在，则自动创建并注册。
        /// </summary>
        /// <param name="note">待追加的音符。</param>
        /// <param name="judgeLineIndex">音符所属判定线的索引。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        private static void AddNoteToDict(
            Note note,
            int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            if (!judgeDict.ContainsKey(judgeLineIndex))
                judgeDict[judgeLineIndex] = new JudgeLine();
            judgeDict[judgeLineIndex].NoteList.Add(note);
        }

        /// <summary>
        /// 对 <paramref name="chart"/> 和 <paramref name="judgeDict"/> 执行最终的排序与组装。
        /// <para>
        /// BPM 列表按起始拍升序排序；每条判定线的关键帧列表、事件列表和音符列表分别按拍数/起始拍升序排序；
        /// 最后将 <paramref name="judgeDict"/> 按判定线索引升序转换为 <see cref="Chart.JudgeLineList"/>。
        /// </para>
        /// </summary>
        /// <param name="chart">待完善的谱面对象，<see cref="Chart.JudgeLineList"/> 将在此方法中赋值。</param>
        /// <param name="judgeDict">解析阶段积累的判定线暂存字典。</param>
        private static void SortAndBuild(Chart chart, Dictionary<int, JudgeLine> judgeDict)
        {
            chart.BpmList = chart.BpmList.OrderBy(b => b.StartBeat).ToList();
            foreach (var judgeLine in judgeDict.Values)
            {
                // 排序
                // Frame
                judgeLine.SpeedFrames = judgeLine.SpeedFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.MoveFrames = judgeLine.MoveFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.RotateFrames = judgeLine.RotateFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.AlphaFrames = judgeLine.AlphaFrames.OrderBy(f => f.Beat).ToList();
                // Event
                judgeLine.MoveEvents = judgeLine.MoveEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.RotateEvents = judgeLine.RotateEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.AlphaEvents = judgeLine.AlphaEvents.OrderBy(e => e.StartBeat).ToList();
                // Note
                judgeLine.NoteList = judgeLine.NoteList.OrderBy(n => n.StartBeat).ToList();
            }

            chart.JudgeLineList = judgeDict.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }
    }
}
