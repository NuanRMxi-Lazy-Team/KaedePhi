using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;

namespace KaedePhi.Core.PhiEdit
{
    public partial class Chart
    {
        private static readonly string[] Separator = { "\r\n", "\n", "\r" };

        /// <summary>
        /// 将 PhiEditChart 格式的文本字符串反序列化为 <see cref="Chart"/> 对象。
        /// <para>
        /// 第一行必须为整数偏移量；随后每行为一条指令（<c>bp</c>、判定线指令或 Note 指令）。
        /// Note 指令若未内联速度/宽度信息，则紧跟的两行分别为速度行（<c># value</c>）和宽度行（<c>&amp; value</c>）。
        /// 解析完成后所有集合按拍数升序排序。
        /// </para>
        /// </summary>
        /// <param name="pec">符合 PhiEditChart 规范的文本字符串。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段数不足。</exception>
        [PublicAPI]
        public static Chart Load(string pec)
        {
            if (pec is null)
                throw new ArgumentNullException(nameof(pec));

            var lines = pec.Split(Separator, StringSplitOptions.None);

            if (!TryParseInteger(lines[0], out var offset))
                throw new FormatException(
                    "Malformed chart file: first line is not a valid integer offset."
                );

            var chart = new Chart { Offset = offset };
            var judgeDict = new Dictionary<int, JudgeLine>();

            var lineIndex = 1;
            while (lineIndex < lines.Length)
                lineIndex = ProcessLine(lines, lineIndex, chart, judgeDict);

            SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 处理 <paramref name="lines"/> 中索引为 <paramref name="i"/> 的单行，并返回下一行的索引。
        /// <para>
        /// 空白行直接跳过（返回 <c>i + 1</c>）；其余行交由
        /// <see cref="ParseChartLineCore(string,string[],int,Chart,Dictionary{int,JudgeLine})"/> 处理。
        /// </para>
        /// </summary>
        /// <param name="lines">谱面全部文本行数组。</param>
        /// <param name="i">当前待处理行的索引（从 1 开始，第 0 行已作为偏移量消耗）。</param>
        /// <param name="chart">正在构建的谱面对象，BPM 列表等数据将就地写入。</param>
        /// <param name="judgeDict">判定线暂存字典，键为判定线索引，值为对应 <see cref="JudgeLine"/>。</param>
        /// <returns>下一次应处理的行索引。</returns>
        /// <exception cref="FormatException">指令字段数不足或格式不合法。</exception>
        private static int ProcessLine(
            string[] lines,
            int i,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                return i + 1;

            return i + ParseChartLineCore(line, lines, i, chart, judgeDict);
        }

        /// <summary>
        /// 从 <paramref name="stream"/> 流式读取 PhiEditChart 并反序列化为 <see cref="Chart"/> 对象。
        /// <para>
        /// 使用 <see cref="StreamReader"/> 逐行读取，内存占用低于 <see cref="Load"/>；
        /// 流读取完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>）。
        /// 解析完成后所有集合按拍数升序排序。
        /// </para>
        /// </summary>
        /// <param name="stream">可读的 PhiEditChart 文件流；调用方负责其生命周期管理。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段数不足。</exception>
        [PublicAPI]
        public static Chart LoadStream(Stream stream)
        {
            using var reader = CreateStreamReader(stream);
            var (chart, judgeDict) = InitializeChart(reader.ReadLine);

            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    ParseChartLineCore(line, reader.ReadLine, chart, judgeDict);
            }

            SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 异步从 <paramref name="stream"/> 流式读取 PhiEditChart 并反序列化为 <see cref="Chart"/> 对象。
        /// <para>
        /// 使用 <see cref="StreamReader"/> 异步逐行读取；
        /// 流读取完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>）。
        /// 解析完成后所有集合按拍数升序排序。
        /// </para>
        /// </summary>
        /// <param name="stream">可读的 PhiEditChart 文件流；调用方负责其生命周期管理。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段数不足。</exception>
        [PublicAPI]
        public static async Task<Chart> LoadStreamAsync(Stream stream)
        {
            using var reader = CreateStreamReader(stream);
            var firstLine = await reader.ReadLineAsync();
            var (chart, judgeDict) = InitializeChart(() => firstLine);

            while (await reader.ReadLineAsync() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    await ParseChartLineAsync(line, reader, chart, judgeDict);
            }

            SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 解析单行谱面指令，并将结果写入 <paramref name="chart"/> 或 <paramref name="judgeDict"/>。
        /// </summary>
        /// <param name="line">要解析的谱面指令行。</param>
        /// <param name="reader">用于读取后续行的 <see cref="StreamReader"/>。</param>
        /// <param name="chart">要写入的 <see cref="Chart"/> 实例。</param>
        /// <param name="judgeDict">要写入的判定线字典。</param>
        /// <exception cref="FormatException">指令行格式错误。</exception>
        private static async Task ParseChartLineAsync(
            string line,
            StreamReader reader,
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
            else if (part[0].StartsWith("n", StringComparison.Ordinal))
            {
                var (speedPart, widthPart) = GetInlineNoteParts(part);
                if (speedPart is null)
                {
                    var speedLine = await reader.ReadLineAsync();
                    var widthLine = await reader.ReadLineAsync();
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
        /// 创建一个 <see cref="StreamReader"/>，用于从指定流中读取谱面文本。
        /// </summary>
        /// <param name="stream">要读取的流。</param>
        /// <returns>用于读取谱面文本的 <see cref="StreamReader"/> 实例。</returns>
        private static StreamReader CreateStreamReader(Stream stream) =>
            new(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                1024,
                leaveOpen: true
            );

        /// <summary>
        /// 初始化谱面对象和判定线字典，并从输入流读取首行偏移量。
        /// </summary>
        /// <param name="readFirstLineFunc">用于读取首行的函数。</param>
        /// <returns>包含初始化后的 <see cref="Chart"/> 和判定线字典的元组。</returns>
        /// <exception cref="FormatException">首行偏移量格式错误。</exception>
        private static (Chart chart, Dictionary<int, JudgeLine> judgeDict) InitializeChart(
            Func<string?> readFirstLineFunc
        )
        {
            var chart = new Chart();
            var judgeDict = new Dictionary<int, JudgeLine>();

            var firstLine = readFirstLineFunc();
            if (!TryParseInteger(firstLine, out var offset))
                throw new FormatException(
                    "Malformed chart file: first line is not a valid integer offset."
                );
            chart.Offset = offset;

            return (chart, judgeDict);
        }
    }
}
