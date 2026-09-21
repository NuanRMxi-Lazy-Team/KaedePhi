using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Formats.PhiEdit.Model;
using KaedePhi.Core.Primitives.Serialization;

namespace KaedePhi.Core.Formats.PhiEdit.Serialization
{
    /// <summary>
    /// 提供 PhiEditChart 格式谱面的文本序列化与反序列化。
    /// </summary>
    public static class ChartSerialization
    {
        private static readonly string[] Separator = { "\r\n", "\n", "\r" };

        /// <summary>
        /// 以惰性迭代方式枚举单条判定线 <paramref name="judgeLine"/> 的所有 PhiEditChart 导出行。
        /// <para>
        /// 输出顺序为：移动关键帧 → 速度关键帧 → 旋转关键帧 → 不透明度关键帧 →
        /// 移动事件 → 旋转事件 → 不透明度事件 → 音符。
        /// </para>
        /// </summary>
        /// <param name="judgeLine">待导出的判定线。</param>
        /// <param name="index">该判定线在谱面中的索引，用于生成指令中的判定线编号字段。</param>
        /// <returns>按 PhiEditChart 规范格式化的文本行序列。</returns>
        private static IEnumerable<string> GetJudgeLineLines(JudgeLine judgeLine, int index)
        {
            // 关键帧按 PhiEditChart 规定的通道顺序输出。
            foreach (var frame in judgeLine.MoveFrames)
                yield return frame.ToString(index);
            foreach (var frame in judgeLine.SpeedFrames)
                yield return frame.ToString(index, "cv");
            foreach (var frame in judgeLine.RotateFrames)
                yield return frame.ToString(index, "cd");
            foreach (var frame in judgeLine.AlphaFrames)
                yield return frame.ToString(index, "ca");

            // 事件按移动、旋转、不透明度的顺序输出。
            foreach (var ev in judgeLine.MoveEvents)
                yield return ev.ToString(index);
            foreach (var ev in judgeLine.RotateEvents)
                yield return ev.ToString(index, "cr");
            foreach (var ev in judgeLine.AlphaEvents)
                yield return ev.ToString(index, "cf");

            foreach (var note in judgeLine.NoteList)
            {
                var (noteLine, speedLine, widthLine) = note.GetExportParts(index);
                yield return noteLine;
                yield return speedLine;
                yield return widthLine;
            }
        }

        /// <summary>
        /// 以惰性迭代方式枚举整个谱面的所有 PhiEditChart 导出行。
        /// <para>输出顺序为：偏移量行 → BPM 行 → 各判定线的全部指令行（调用 <see cref="GetJudgeLineLines"/>）。</para>
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <returns>按 PhiEditChart 规范格式化的完整谱面文本行序列。</returns>
        private static IEnumerable<string> GetExportLines(Chart chart)
        {
            yield return chart.Offset.ToString(CultureInfo.InvariantCulture);
            foreach (var bpm in chart.BpmList)
                yield return bpm.ToString();
            for (var i = 0; i < chart.JudgeLineList.Count; i++)
                foreach (var line in GetJudgeLineLines(chart.JudgeLineList[i], i))
                    yield return line;
        }

        /// <summary>
        /// 将谱面序列化为 PhiEditChart 格式的文本字符串，各行以 <see cref="Environment.NewLine"/> 连接。
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <returns>完整的 PhiEditChart 文本。</returns>
        [PublicAPI]
        public static string Export(this Chart chart) =>
            string.Join(Environment.NewLine, GetExportLines(chart));

        /// <summary>
        /// 将谱面序列化为 PhiEditChart 格式的文本字符串。
        /// <para>序列化为 CPU 密集的同步操作，直接返回已完成任务，不做线程池假异步。</para>
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <returns>完整的 PhiEditChart 文本。</returns>
        public static Task<string> ExportAsync(this Chart chart) => Task.FromResult(chart.Export());

        /// <summary>
        /// 将谱面以 PhiEditChart 格式流式写入 <paramref name="stream"/>，每行结尾使用系统换行符。
        /// <para>写入完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>），调用方负责其生命周期管理。</para>
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <param name="stream">可写的目标流。</param>
        public static void ExportToStream(this Chart chart, Stream stream)
        {
            using var writer = CreateStreamWriter(stream);
            WriteExportLines(chart, writer.WriteLine);
        }

        /// <summary>
        /// 将谱面以 PhiEditChart 格式异步流式写入 <paramref name="stream"/>，每行结尾使用系统换行符。
        /// <para>写入完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>），调用方负责其生命周期管理。</para>
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <param name="stream">可写的目标流。</param>
        public static async Task ExportToStreamAsync(this Chart chart, Stream stream)
        {
            await using var writer = CreateStreamWriter(stream);
            await WriteExportLinesAsync(chart, writer).ConfigureAwait(false);
        }

        /// <summary>
        /// 创建一个 <see cref="StreamWriter"/>，使用 UTF-8 无 BOM 编码，缓冲区大小为 1024 字节，并在写入完成后不关闭底层流。
        /// </summary>
        /// <param name="stream">要写入的流。</param>
        /// <returns>用于写入谱面文本的 <see cref="StreamWriter"/> 实例。</returns>
        private static StreamWriter CreateStreamWriter(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable.", nameof(stream));

            return new StreamWriter(stream, JsonDefaults.NoBomUtf8, 1024, leaveOpen: true);
        }

        /// <summary>
        /// 将谱面以 PhiEditChart 格式写入指定的行写入函数。
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <param name="writeLineFunc">用于写入每一行的函数。</param>
        private static void WriteExportLines(Chart chart, Action<string> writeLineFunc)
        {
            foreach (var line in GetExportLines(chart))
                writeLineFunc(line);
        }

        /// <summary>
        /// 异步写出共享的导出行序列，避免同步和异步导出分别维护一套遍历逻辑。
        /// </summary>
        /// <param name="chart">待导出的谱面。</param>
        /// <param name="writer">目标文本写入器。</param>
        private static async Task WriteExportLinesAsync(Chart chart, StreamWriter writer)
        {
            foreach (var line in GetExportLines(chart))
                await writer.WriteLineAsync(line).ConfigureAwait(false);
        }

        /// <summary>
        /// 将 PhiEditChart 格式的文本字符串反序列化为 <see cref="Chart"/> 对象。
        /// </summary>
        /// <param name="pec">符合 PhiEditChart 规范的文本字符串。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pec"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段格式错误。</exception>
        [PublicAPI]
        public static Chart Load(string pec)
        {
            if (pec is null)
                throw new ArgumentNullException(nameof(pec));

            var lines = pec.Split(Separator, StringSplitOptions.None);
            var (chart, judgeDict) = ChartParser.InitializeChart(lines[0]);
            var lineIndex = 1;

            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex++];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var pendingNote = ChartParser.ParseChartLineCore(line, chart, judgeDict);
                if (pendingNote is not { } pending)
                    continue;

                if (lineIndex + 1 >= lines.Length)
                    throw new FormatException(
                        $"Malformed note at line {lineIndex}: missing speed or width lines."
                    );

                ChartParser.CompletePendingNote(
                    pending,
                    lines[lineIndex],
                    lines[lineIndex + 1],
                    judgeDict,
                    "Malformed note: missing speed or width lines."
                );
                lineIndex += 2;
            }

            ChartParser.SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 将 PhiEditChart 格式的文本字符串反序列化为 <see cref="Chart"/> 对象。
        /// 字符串解析是 CPU 密集操作，因此该方法直接返回已完成任务，不在线程池上重复调度。
        /// </summary>
        /// <param name="pec">符合 PhiEditChart 规范的文本字符串。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        public static Task<Chart> LoadAsync(string pec) => Task.FromResult(Load(pec));

        /// <summary>
        /// 从 <paramref name="stream"/> 流式读取 PhiEditChart 并反序列化为 <see cref="Chart"/> 对象。
        /// 读取完毕后不会关闭输入流。
        /// </summary>
        /// <param name="stream">可读的 PhiEditChart 文件流；调用方负责其生命周期管理。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> 不可读。</exception>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段格式错误。</exception>
        [PublicAPI]
        public static Chart LoadStream(Stream stream)
        {
            using var reader = CreateStreamReader(stream);
            var (chart, judgeDict) = ChartParser.InitializeChart(reader.ReadLine());

            while (reader.ReadLine() is { } line)
                ParseStreamLine(line, reader, chart, judgeDict);

            ChartParser.SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 异步从 <paramref name="stream"/> 流式读取 PhiEditChart 并反序列化为 <see cref="Chart"/> 对象。
        /// 读取完毕后不会关闭输入流。
        /// </summary>
        /// <param name="stream">可读的 PhiEditChart 文件流；调用方负责其生命周期管理。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="stream"/> 不可读。</exception>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段格式错误。</exception>
        [PublicAPI]
        public static async Task<Chart> LoadStreamAsync(Stream stream)
        {
            using var reader = CreateStreamReader(stream);
            var firstLine = await reader.ReadLineAsync().ConfigureAwait(false);
            var (chart, judgeDict) = ChartParser.InitializeChart(firstLine);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
                await ParseStreamLineAsync(line, reader, chart, judgeDict).ConfigureAwait(false);

            ChartParser.SortAndBuild(chart, judgeDict);
            return chart;
        }

        /// <summary>
        /// 处理同步流中的一行，并在 Note 使用多行格式时继续消费其参数行。
        /// </summary>
        /// <param name="line">当前读取到的文本行。</param>
        /// <param name="reader">输入流读取器。</param>
        /// <param name="chart">正在构建的谱面。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        private static void ParseStreamLine(
            string line,
            StreamReader reader,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            var pendingNote = ChartParser.ParseChartLineCore(line, chart, judgeDict);
            if (pendingNote is not { } pending)
                return;

            ChartParser.CompletePendingNote(
                pending,
                reader.ReadLine(),
                reader.ReadLine(),
                judgeDict,
                "Malformed note: missing speed or width lines."
            );
        }

        /// <summary>
        /// 处理异步流中的一行，并在 Note 使用多行格式时继续消费其参数行。
        /// </summary>
        /// <param name="line">当前读取到的文本行。</param>
        /// <param name="reader">输入流读取器。</param>
        /// <param name="chart">正在构建的谱面。</param>
        /// <param name="judgeDict">判定线暂存字典。</param>
        private static async Task ParseStreamLineAsync(
            string line,
            StreamReader reader,
            Chart chart,
            Dictionary<int, JudgeLine> judgeDict
        )
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            var pendingNote = ChartParser.ParseChartLineCore(line, chart, judgeDict);
            if (pendingNote is not { } pending)
                return;

            var speedLine = await reader.ReadLineAsync().ConfigureAwait(false);
            var widthLine = await reader.ReadLineAsync().ConfigureAwait(false);
            ChartParser.CompletePendingNote(
                pending,
                speedLine,
                widthLine,
                judgeDict,
                "Malformed note: missing speed or width lines."
            );
        }

        /// <summary>
        /// 创建使用 UTF-8 无 BOM 编码的输入读取器，并保留底层流的所有权。
        /// </summary>
        /// <param name="stream">要读取的输入流。</param>
        /// <returns>用于读取谱面文本的 <see cref="StreamReader"/>。</returns>
        private static StreamReader CreateStreamReader(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(stream));

            return new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true
            );
        }
    }
}