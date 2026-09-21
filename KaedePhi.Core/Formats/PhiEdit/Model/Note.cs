using System;
using System.Globalization;

namespace KaedePhi.Core.Formats.PhiEdit.Model
{
    public class Note
    {
        public NoteType Type { get; set; } = NoteType.Tap;
        public float StartBeat { get; set; }
        public float EndBeat { get; set; } // 仅 Hold 使用
        public bool Above { get; set; } = true;
        public float PositionX { get; set; }
        public float WidthRatio { get; set; } = 1.0f;
        public bool IsFake { get; set; }
        public float SpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int)"/>
        /// </summary>
        public override string ToString() =>
            $"Note(Type: {Type}, StartBeat: {StartBeat}, EndBeat: {EndBeat}, Above: {Above}, PositionX: {PositionX}, WidthRatio: {WidthRatio}, IsFake: {IsFake}, SpeedMultiplier: {SpeedMultiplier})";

        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        /// <exception cref="ArgumentException">存储的数值有误</exception>
        public string ToString(int judgeLineIndex)
        {
            var (noteLine, speedLine, widthLine) = GetExportParts(judgeLineIndex);
            return string.Join(Environment.NewLine, noteLine, speedLine, widthLine);
        }

        /// <summary>
        /// 获取 Note 导出的三条物理文本行，供字符串导出和流式导出共享。
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引。</param>
        /// <returns>Note 主指令、速度倍率行和宽度比例行。</returns>
        /// <exception cref="ArgumentException">存储的数值有误。</exception>
        internal (string noteLine, string speedLine, string widthLine) GetExportParts(
            int judgeLineIndex
        )
        {
            const int fakeNote = 1;
            const int realNote = 0;
            const int aboveNote = 1;
            const int belowNote = 2;

            if (Type != NoteType.Hold && Math.Abs(StartBeat - EndBeat) > 0.0001f)
                throw new ArgumentException("非Hold音符的开始拍与结束拍应相等");

            var aboveNumber = Above ? aboveNote : belowNote;
            var isFakeNumber = IsFake ? fakeNote : realNote;
            var noteLine = Type == NoteType.Hold
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2} {3} {4} {5} {6}",
                    $"n{(int)Type}",
                    judgeLineIndex,
                    StartBeat,
                    EndBeat,
                    PositionX,
                    aboveNumber,
                    isFakeNumber
                )
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2} {3} {4} {5}",
                    $"n{(int)Type}",
                    judgeLineIndex,
                    StartBeat,
                    PositionX,
                    aboveNumber,
                    isFakeNumber
                );

            return (
                noteLine,
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", "#", SpeedMultiplier),
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", "&", WidthRatio)
            );
        }

        public Note Clone()
        {
            return new Note
            {
                Type = Type,
                StartBeat = StartBeat,
                EndBeat = EndBeat,
                Above = Above,
                PositionX = PositionX,
                WidthRatio = WidthRatio,
                IsFake = IsFake,
                SpeedMultiplier = SpeedMultiplier,
            };
        }
    }

    public enum NoteType
    {
        Tap = 1,
        Hold = 2,
        Flick = 3,
        Drag = 4,
    }
}