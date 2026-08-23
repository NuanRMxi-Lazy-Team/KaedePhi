using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;

namespace KaedePhi.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 将 PhiEditChart 格式的文本字符串反序列化为 <see cref="Chart"/> 对象。
        /// <para>反序列化为 CPU 密集的同步操作，直接返回已完成任务，不做线程池假异步。</para>
        /// </summary>
        /// <param name="pec">符合 PhiEditChart 规范的文本字符串。</param>
        /// <returns>已完整反序列化并排序的 <see cref="Chart"/> 实例。</returns>
        /// <exception cref="FormatException">首行不是合法整数偏移量，或任意指令字段数不足。</exception>
        public static Task<Chart> LoadAsync(string pec) => Task.FromResult(Load(pec));

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
            // Frame
            foreach (var frame in judgeLine.MoveFrames)
                yield return frame.ToString(index);
            foreach (var frame in judgeLine.SpeedFrames)
                yield return frame.ToString(index, "cv");
            foreach (var frame in judgeLine.RotateFrames)
                yield return frame.ToString(index, "cd");
            foreach (var frame in judgeLine.AlphaFrames)
                yield return frame.ToString(index, "ca");
            // Event
            foreach (var ev in judgeLine.MoveEvents)
                yield return ev.ToString(index);
            foreach (var ev in judgeLine.RotateEvents)
                yield return ev.ToString(index, "cr");
            foreach (var ev in judgeLine.AlphaEvents)
                yield return ev.ToString(index, "cf");
            // Note
            foreach (var note in judgeLine.NoteList)
                yield return note.ToString(index);
        }

        /// <summary>
        /// 以惰性迭代方式枚举整个谱面的所有 PhiEditChart 导出行。
        /// <para>输出顺序为：偏移量行 → BPM 行 → 各判定线的全部指令行（调用 <see cref="GetJudgeLineLines"/>）。</para>
        /// </summary>
        /// <returns>按 PhiEditChart 规范格式化的完整谱面文本行序列。</returns>
        private IEnumerable<string> GetExportLines()
        {
            yield return Offset.ToString(CultureInfo.InvariantCulture);
            foreach (var bpm in BpmList)
                yield return bpm.ToString();
            for (var i = 0; i < JudgeLineList.Count; i++)
                foreach (var line in GetJudgeLineLines(JudgeLineList[i], i))
                    yield return line;
        }

        /// <summary>
        /// 将谱面序列化为 PhiEditChart 格式的文本字符串，各行以 <see cref="Environment.NewLine"/> 连接。
        /// </summary>
        /// <returns>完整的 PhiEditChart 文本。</returns>
        [PublicAPI]
        public string Export() => string.Join(Environment.NewLine, GetExportLines());

        /// <summary>
        /// 将谱面序列化为 PhiEditChart 格式的文本字符串。
        /// <para>序列化为 CPU 密集的同步操作，直接返回已完成任务，不做线程池假异步。</para>
        /// </summary>
        /// <returns>完整的 PhiEditChart 文本。</returns>
        public Task<string> ExportAsync() => Task.FromResult(Export());

        /// <summary>
        /// 将谱面以 PhiEditChart 格式流式写入 <paramref name="stream"/>，每行结尾使用系统换行符。
        /// <para>写入完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>），调用方负责其生命周期管理。</para>
        /// </summary>
        /// <param name="stream">可写的目标流。</param>
        public void ExportToStream(Stream stream)
        {
            using var writer = CreateStreamWriter(stream);
            WriteExportLines(writer.WriteLine);
        }

        /// <summary>
        /// 将谱面以 PhiEditChart 格式异步流式写入 <paramref name="stream"/>，每行结尾使用系统换行符。
        /// <para>写入完毕后不会关闭 <paramref name="stream"/>（<c>leaveOpen: true</c>），调用方负责其生命周期管理。</para>
        /// </summary>
        /// <param name="stream">可写的目标流。</param>
        public async Task ExportToStreamAsync(Stream stream)
        {
            await using var writer = CreateStreamWriter(stream);
            foreach (var line in GetExportLines())
                await writer.WriteLineAsync(line);
        }

        /// <summary>
        /// 创建一个 <see cref="StreamWriter"/>，使用 UTF-8 无 BOM 编码，缓冲区大小为 1024 字节，并在写入完成后不关闭底层流。
        /// </summary>
        /// <param name="stream">要写入的流。</param>
        /// <returns>用于写入谱面文本的 <see cref="StreamWriter"/> 实例。</returns>
        private static StreamWriter CreateStreamWriter(Stream stream) =>
            new(stream, JsonDefaults.NoBomUtf8, 1024, leaveOpen: true);

        /// <summary>
        /// 将谱面以 PhiEditChart 格式写入指定的行写入函数。
        /// </summary>
        /// <param name="writeLineFunc">用于写入每一行的函数。</param>
        private void WriteExportLines(Action<string> writeLineFunc)
        {
            foreach (var line in GetExportLines())
                writeLineFunc(line);
        }
    }
}
