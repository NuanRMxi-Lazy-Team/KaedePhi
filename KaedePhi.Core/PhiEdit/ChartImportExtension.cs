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
            var (chart, judgeDict) = InitializeChart(lines[0]);
            var lineIndex = 1;

            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex++];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var pendingNote = ParseChartLineCore(line, chart, judgeDict);
                if (pendingNote is not { } pending)
                    continue;

                if (lineIndex + 1 >= lines.Length)
                    throw new FormatException(
                        $"Malformed note at line {lineIndex}: missing speed or width lines."
                    );

                CompletePendingNote(
                    pending,
                    lines[lineIndex],
                    lines[lineIndex + 1],
                    judgeDict,
                    "Malformed note: missing speed or width lines."
                );
                lineIndex += 2;
            }

            SortAndBuild(chart, judgeDict);
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
            var (chart, judgeDict) = InitializeChart(reader.ReadLine());

            while (reader.ReadLine() is { } line)
                ParseStreamLine(line, reader, chart, judgeDict);

            SortAndBuild(chart, judgeDict);
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
            var (chart, judgeDict) = InitializeChart(firstLine);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
                await ParseStreamLineAsync(line, reader, chart, judgeDict).ConfigureAwait(false);

            SortAndBuild(chart, judgeDict);
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

            var pendingNote = ParseChartLineCore(line, chart, judgeDict);
            if (pendingNote is not { } pending)
                return;

            CompletePendingNote(
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

            var pendingNote = ParseChartLineCore(line, chart, judgeDict);
            if (pendingNote is not { } pending)
                return;

            var speedLine = await reader.ReadLineAsync().ConfigureAwait(false);
            var widthLine = await reader.ReadLineAsync().ConfigureAwait(false);
            CompletePendingNote(
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