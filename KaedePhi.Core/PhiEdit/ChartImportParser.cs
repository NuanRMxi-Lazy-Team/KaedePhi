using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KaedePhi.Core.PhiEdit
{
    public partial class Chart
    {
        private readonly struct NotePart
        {
            public NotePart(string marker, string value)
            {
                Marker = marker;
                Value = value;
            }

            public string Marker { get; }
            public string Value { get; }
        }

        private readonly struct PendingNote
        {
            public PendingNote(string[] commandParts, int judgeLineIndex)
            {
                CommandParts = commandParts;
                JudgeLineIndex = judgeLineIndex;
            }

            public string[] CommandParts { get; }
            public int JudgeLineIndex { get; }
        }

        /// <summary>
        /// 解析一条指令。只有缺少内联速度和宽度信息的 Note 才返回待续行状态。
        /// </summary>
        /// <param name="line">当前非空白文本行。</param>
        /// <param name="chart">正在构建的谱面。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <returns>需要继续读取两行参数的 Note；其他指令返回 <see langword="null"/>。</returns>
        /// <exception cref="FormatException">指令字段数不足或字段格式错误。</exception>
        private static PendingNote? ParseChartLineCore(
            string line,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            var parts = SplitWhitespace(line);
            var judgeLineIndex = GetJudgeLineIndex(parts);

            if (parts[0] == "bp")
            {
                EnsureMinParts(parts, 3, "bp");
                chart.BpmList.Add(
                    new BpmItem
                    {
                        StartBeat = ParseFloat(parts[1], "bp 起始拍"),
                        Bpm = ParseFloat(parts[2], "bp BPM"),
                    }
                );
                return null;
            }

            if (parts[0].StartsWith('n'))
            {
                if (TryGetInlineNoteParts(parts, out var speedPart, out var widthPart))
                {
                    AddNoteToDict(
                        BuildNote(parts, speedPart, widthPart),
                        judgeLineIndex,
                        judgeDict
                    );
                    return null;
                }

                return new PendingNote(parts, judgeLineIndex);
            }

            ParseLineCommand(parts, judgeLineIndex, judgeDict);
            return null;
        }

        /// <summary>
        /// 使用已经读取的速度行和宽度行完成一个多行 Note。
        /// </summary>
        /// <param name="pending">待完成的 Note 信息。</param>
        /// <param name="speedLine">速度倍率行。</param>
        /// <param name="widthLine">宽度比例行。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <param name="missingLinesMessage">续行缺失时使用的错误消息。</param>
        /// <exception cref="FormatException">续行缺失或字段格式错误。</exception>
        private static void CompletePendingNote(
            PendingNote pending,
            string? speedLine,
            string? widthLine,
            Dictionary<int, JudgeLine> judgeDict,
            string missingLinesMessage
        )
        {
            if (speedLine is null || widthLine is null)
                throw new FormatException(missingLinesMessage);

            var speedPart = ParseNotePart(speedLine, isSpeedPart: true);
            var widthPart = ParseNotePart(widthLine, isSpeedPart: false);
            AddNoteToDict(
                BuildNote(pending.CommandParts, speedPart, widthPart),
                pending.JudgeLineIndex,
                judgeDict
            );
        }

        /// <summary>
        /// 校验指令的字段数量是否满足最低要求；不满足时抛出包含命令名称和实际/期望字段数的 <see cref="FormatException"/>。
        /// </summary>
        /// <param name="parts">已按空格拆分的指令字段数组。</param>
        /// <param name="min">该指令要求的最小字段数（含指令标识符本身）。</param>
        /// <param name="command">指令名称，用于生成错误消息。</param>
        /// <exception cref="FormatException"><paramref name="parts"/> 的长度小于 <paramref name="min"/>。</exception>
        private static void EnsureMinParts(string[] parts, int min, string command)
        {
            if (parts.Length < min)
                throw new FormatException(
                    $"Malformed '{command}' command: expected at least {min} parts, got {parts.Length}."
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
        /// 解析指令的判定线索引字段；BPM 指令不属于任何判定线。
        /// </summary>
        /// <param name="parts">已按空格拆分的指令字段数组。</param>
        /// <returns>判定线索引，BPM 指令返回 -1。</returns>
        /// <exception cref="FormatException"><paramref name="parts"/> 的长度不足或格式错误。</exception>
        private static int GetJudgeLineIndex(string[] parts)
        {
            if (parts.Length == 0)
                throw new FormatException("Malformed chart command: command is empty.");
            if (parts[0] == "bp")
                return -1;

            EnsureMinParts(parts, 2, parts[0]);
            return ParseInteger(parts[1], $"{parts[0]} 判定线索引");
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
        /// 解析一个整数并在失败时生成包含字段名称的格式异常。
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
        /// 解析一个有限浮点数并在失败时生成包含字段名称的格式异常。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="field">字段名称，用于生成错误消息。</param>
        /// <returns>解析成功的浮点数值。</returns>
        /// <exception cref="FormatException">解析失败或结果不是有限值。</exception>
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
        /// 解析 0/1 二进制标记。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <param name="field">字段名称，用于生成错误消息。</param>
        /// <returns>解析成功的布尔值。</returns>
        /// <exception cref="FormatException">文本不是 0 或 1。</exception>
        private static bool ParseBinaryFlag(string text, string field) =>
            text switch
            {
                "0" => false,
                "1" => true,
                _ => throw new FormatException($"Malformed chart field '{field}': '{text}'."),
            };

        /// <summary>
        /// 解析 1/2 上下侧标记。
        /// </summary>
        /// <param name="text">要解析的文本。</param>
        /// <returns>解析成功的布尔值。</returns>
        /// <exception cref="FormatException">文本不是 1 或 2。</exception>
        private static bool ParseAboveFlag(string text) =>
            text switch
            {
                "1" => true,
                "2" => false,
                _ => throw new FormatException("Malformed note field 'note 上下侧'."),
            };

        /// <summary>
        /// 根据指令类型解析关键帧或事件，并追加到对应判定线。
        /// 未知指令保持静默忽略，以兼容 PhiEdit 的扩展指令。
        /// </summary>
        /// <param name="parts">已按空格拆分的指令字段数组。</param>
        /// <param name="judgeLineIndex">当前指令作用的判定线索引。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <exception cref="FormatException">指令字段数不足。</exception>
        private static void ParseLineCommand(
            string[] parts,
            int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            switch (parts[0])
            {
                case "cv":
                case "cd":
                case "ca":
                {
                    EnsureMinParts(parts, 4, parts[0]);
                    var frame = new Frame
                    {
                        Beat = ParseFloat(parts[2], $"{parts[0]} 拍数"),
                        Value = ParseFloat(parts[3], $"{parts[0]} 数值"),
                    };
                    var judgeLine = GetOrCreateJudgeLine(judgeLineIndex, judgeDict);
                    if (parts[0] == "cv")
                        judgeLine.SpeedFrames.Add(frame);
                    else if (parts[0] == "cd")
                        judgeLine.RotateFrames.Add(frame);
                    else
                        judgeLine.AlphaFrames.Add(frame);
                    break;
                }
                case "cp":
                {
                    EnsureMinParts(parts, 5, "cp");
                    GetOrCreateJudgeLine(judgeLineIndex, judgeDict).MoveFrames.Add(
                        new MoveFrame
                        {
                            Beat = ParseFloat(parts[2], "cp 拍数"),
                            XValue = ParseFloat(parts[3], "cp X 数值"),
                            YValue = ParseFloat(parts[4], "cp Y 数值"),
                        }
                    );
                    break;
                }
                case "cm":
                {
                    EnsureMinParts(parts, 7, "cm");
                    GetOrCreateJudgeLine(judgeLineIndex, judgeDict).MoveEvents.Add(
                        new MoveEvent
                        {
                            StartBeat = ParseFloat(parts[2], "cm 起始拍"),
                            EndBeat = ParseFloat(parts[3], "cm 结束拍"),
                            EndXValue = ParseFloat(parts[4], "cm X 数值"),
                            EndYValue = ParseFloat(parts[5], "cm Y 数值"),
                            EasingType = Easing.Get(ParseInteger(parts[6], "cm 缓动类型")),
                        }
                    );
                    break;
                }
                case "cr":
                case "cf":
                {
                    var isRotate = parts[0] == "cr";
                    EnsureMinParts(parts, isRotate ? 6 : 5, parts[0]);
                    var eventValue = new Event
                    {
                        StartBeat = ParseFloat(parts[2], $"{parts[0]} 起始拍"),
                        EndBeat = ParseFloat(parts[3], $"{parts[0]} 结束拍"),
                        EndValue = ParseFloat(parts[4], $"{parts[0]} 数值"),
                        EasingType = isRotate
                            ? Easing.Get(ParseInteger(parts[5], "cr 缓动类型"))
                            : Easing.Linear,
                    };
                    var judgeLine = GetOrCreateJudgeLine(judgeLineIndex, judgeDict);
                    if (isRotate)
                        judgeLine.RotateEvents.Add(eventValue);
                    else
                        judgeLine.AlphaEvents.Add(eventValue);
                    break;
                }
            }
        }

        /// <summary>
        /// 根据已拆分的字段数组构造一个 Note 对象。
        /// </summary>
        /// <param name="parts">Note 主指令字段数组。</param>
        /// <param name="speedPart">速度倍率字段。</param>
        /// <param name="widthPart">宽度比例字段。</param>
        /// <returns>完整填充的 Note 实例。</returns>
        /// <exception cref="FormatException">任意字段数组元素数量不足或格式错误。</exception>
        private static Note BuildNote(string[] parts, NotePart speedPart, NotePart widthPart)
        {
            if (
                parts[0].Length < 2
                || !int.TryParse(
                    parts[0][1..],
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
            var inlineMarkerIndex = Array.IndexOf(parts, "#");
            var notePartCount = inlineMarkerIndex >= 0 ? inlineMarkerIndex : parts.Length;
            if (notePartCount < requiredPartCount)
                throw new FormatException(
                    $"Malformed 'note' command: expected at least {requiredPartCount} parts, got {notePartCount}."
                );
            if (speedPart.Marker != "#" || widthPart.Marker != "&")
                throw new FormatException("Malformed note: invalid speed or width marker.");

            var startBeat = ParseFloat(parts[2], "note 起始拍");
            var endBeat = isHold ? ParseFloat(parts[3], "note 结束拍") : startBeat;
            if (isHold && endBeat <= startBeat)
                throw new FormatException("Hold 音符的结束拍必须晚于开始拍。");

            return new Note
            {
                StartBeat = startBeat,
                EndBeat = endBeat,
                PositionX = ParseFloat(parts[isHold ? 4 : 3], "note X 坐标"),
                Above = ParseAboveFlag(parts[isHold ? 5 : 4]),
                IsFake = ParseBinaryFlag(parts[isHold ? 6 : 5], "note 假音符标记"),
                SpeedMultiplier = ParseFloat(speedPart.Value, "note 速度倍率"),
                WidthRatio = ParseFloat(widthPart.Value, "note 宽度比例"),
                Type = noteType,
            };
        }

        /// <summary>
        /// 解析独立的 Note 速度或宽度行。
        /// </summary>
        /// <param name="line">参数行文本。</param>
        /// <param name="isSpeedPart">是否为速度倍率行。</param>
        /// <returns>参数行的标记和值。</returns>
        /// <exception cref="FormatException">参数行字段不足。</exception>
        private static NotePart ParseNotePart(string line, bool isSpeedPart)
        {
            var parts = SplitWhitespace(line);
            if (parts.Length < 2)
                throw new FormatException(
                    isSpeedPart
                        ? "Malformed note speed multiplier part: expected at least 2 elements."
                        : "Malformed note width ratio part: expected at least 2 elements."
                );
            return new NotePart(parts[0], parts[1]);
        }

        /// <summary>
        /// 从 Note 主指令中尝试读取内联的速度和宽度参数。
        /// </summary>
        /// <param name="parts">Note 行按空格拆分后的字段。</param>
        /// <param name="speedPart">读取到的速度倍率字段。</param>
        /// <param name="widthPart">读取到的宽度比例字段。</param>
        /// <returns>同时找到两个完整内联参数时返回 <c>true</c>。</returns>
        private static bool TryGetInlineNoteParts(
            string[] parts,
            out NotePart speedPart,
            out NotePart widthPart
        )
        {
            var hashIndex = Array.IndexOf(parts, "#");
            var ampIndex = Array.IndexOf(parts, "&");
            if (
                hashIndex >= 0
                && ampIndex >= 0
                && hashIndex + 1 < parts.Length
                && ampIndex + 1 < parts.Length
            )
            {
                speedPart = new NotePart("#", parts[hashIndex + 1]);
                widthPart = new NotePart("&", parts[ampIndex + 1]);
                return true;
            }

            speedPart = default;
            widthPart = default;
            return false;
        }

        /// <summary>
        /// 获取指定判定线，若尚不存在则创建并登记。
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        /// <returns>对应的判定线实例。</returns>
        private static JudgeLine GetOrCreateJudgeLine(
            int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            if (!judgeDict.TryGetValue(judgeLineIndex, out var judgeLine))
            {
                judgeLine = new JudgeLine();
                judgeDict.Add(judgeLineIndex, judgeLine);
            }

            return judgeLine;
        }

        /// <summary>
        /// 将 Note 追加到指定判定线。
        /// </summary>
        /// <param name="note">待追加的音符。</param>
        /// <param name="judgeLineIndex">音符所属判定线索引。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        private static void AddNoteToDict(
            Note note,
            int judgeLineIndex,
            Dictionary<int, JudgeLine> judgeDict
        ) => GetOrCreateJudgeLine(judgeLineIndex, judgeDict).NoteList.Add(note);

        /// <summary>
        /// 初始化谱面对象和判定线字典，并解析首行偏移量。
        /// </summary>
        /// <param name="firstLine">谱面首行。</param>
        /// <returns>初始化后的谱面和判定线字典。</returns>
        /// <exception cref="FormatException">首行偏移量格式错误。</exception>
        private static (Chart chart, Dictionary<int, JudgeLine> judgeDict) InitializeChart(
            string? firstLine
        )
        {
            if (!TryParseInteger(firstLine?.TrimStart('\uFEFF'), out var offset))
                throw new FormatException(
                    "Malformed chart file: first line is not a valid integer offset."
                );

            return (new Chart { Offset = offset }, new Dictionary<int, JudgeLine>());
        }

        /// <summary>
        /// 对解析结果执行稳定排序，并将判定线字典转换为列表。
        /// </summary>
        /// <param name="chart">待完善的谱面对象。</param>
        /// <param name="judgeDict">解析阶段积累的判定线暂存字典。</param>
        private static void SortAndBuild(Chart chart, Dictionary<int, JudgeLine> judgeDict)
        {
            chart.BpmList = SortByBeat(chart.BpmList, static bpm => bpm.StartBeat);
            foreach (var judgeLine in judgeDict.Values)
            {
                judgeLine.SpeedFrames = SortByBeat(
                    judgeLine.SpeedFrames,
                    static frame => frame.Beat
                );
                judgeLine.MoveFrames = SortByBeat(
                    judgeLine.MoveFrames,
                    static frame => frame.Beat
                );
                judgeLine.RotateFrames = SortByBeat(
                    judgeLine.RotateFrames,
                    static frame => frame.Beat
                );
                judgeLine.AlphaFrames = SortByBeat(
                    judgeLine.AlphaFrames,
                    static frame => frame.Beat
                );
                judgeLine.MoveEvents = SortByBeat(
                    judgeLine.MoveEvents,
                    static eventValue => eventValue.StartBeat
                );
                judgeLine.RotateEvents = SortByBeat(
                    judgeLine.RotateEvents,
                    static eventValue => eventValue.StartBeat
                );
                judgeLine.AlphaEvents = SortByBeat(
                    judgeLine.AlphaEvents,
                    static eventValue => eventValue.StartBeat
                );
                judgeLine.NoteList = SortByBeat(
                    judgeLine.NoteList,
                    static note => note.StartBeat
                );
            }

            chart.JudgeLineList = judgeDict
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value)
                .ToList();
        }

        /// <summary>
        /// 对拍点列表执行稳定排序；若输入已经有序则直接复用原列表。
        /// </summary>
        /// <typeparam name="T">列表元素类型。</typeparam>
        /// <param name="items">待排序列表。</param>
        /// <param name="getBeat">获取元素排序拍点的函数。</param>
        /// <returns>排序后的列表。</returns>
        private static List<T> SortByBeat<T>(List<T> items, Func<T, float> getBeat)
        {
            for (var i = 1; i < items.Count; i++)
            {
                if (getBeat(items[i - 1]) > getBeat(items[i]))
                    return items.OrderBy(getBeat).ToList();
            }

            return items;
        }
    }
}