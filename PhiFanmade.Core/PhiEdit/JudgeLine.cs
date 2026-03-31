using System;
using System.Collections.Generic;
using System.Linq;

namespace PhiFanmade.Core.PhiEdit
{
    public class JudgeLine
    {
        public List<Frame> SpeedFrames { get; set; } = new();
        public List<MoveFrame> MoveFrames { get; set; } = new();
        public List<Frame> RotateFrames { get; set; } = new();
        public List<Frame> AlphaFrames { get; set; } = new();

        public List<Event> AlphaEvents { get; set; } = new();
        public List<MoveEvent> MoveEvents { get; set; } = new();
        public List<Event> RotateEvents { get; set; } = new();

        public List<Note> NoteList { get; set; } = new();

        /// <summary>
        /// 获取指定拍点上的判定线移动坐标。
        /// 优先级：精确匹配 Frame -> 当前生效 Event 插值 -> 最近的历史 Event/Frame -> 默认 (0, 0)。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>该拍点对应的 (X, Y) 坐标。</returns>
        public (float, float) GetMoveAtBeat(float beat)
        {
            var (foundExactFrame, exactFrameValue, previousFrame) = FindExactOrPreviousFrame(beat);
            if (foundExactFrame)
                return exactFrameValue;

            var activeEvent = FindActiveMoveEvent(beat);
            if (activeEvent != null)
            {
                var (startX, startY) = ResolveMoveEventStartValue(beat, previousFrame);
                return activeEvent.GetValueAtBeat(beat, startX, startY);
            }

            var previousEvent = FindPreviousMoveEvent(beat);
            if (IsEventCloserThanFrame(previousEvent, previousFrame))
                return (previousEvent.EndXValue, previousEvent.EndYValue);

            if (previousFrame != null)
                return (previousFrame.XValue, previousFrame.YValue);

            return (0, 0);
        }

        /// <summary>
        /// 在 MoveFrames 中查找与目标拍点精确匹配的帧，或返回最近的前置帧。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>
        /// foundExactFrame 表示是否命中精确帧；
        /// value 为精确帧值（未命中时无意义）；
        /// previousFrame 为最近的前置帧（不存在则为 null）。
        /// </returns>
        private (bool foundExactFrame, (float, float) value, MoveFrame previousFrame) FindExactOrPreviousFrame(float beat)
        {
            for (int i = MoveFrames.Count - 1; i >= 0; i--)
            {
                var frame = MoveFrames[i];
                if (Math.Abs(frame.Beat - beat) < 0.0001f)
                    return (true, (frame.XValue, frame.YValue), frame);

                if (frame.Beat < beat)
                    return (false, default, frame);
            }

            return (false, default, null);
        }

        /// <summary>
        /// 查找目标拍点上正在生效的移动事件。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>命中的移动事件；若不存在则返回 null。</returns>
        private MoveEvent FindActiveMoveEvent(float beat)
        {
            for (int i = 0; i < MoveEvents.Count; i++)
            {
                var e = MoveEvents[i];
                if (beat >= e.StartBeat && beat <= e.EndBeat)
                    return e;

                if (beat < e.StartBeat)
                    break;
            }

            return null;
        }

        /// <summary>
        /// 查找目标拍点之前最近结束的移动事件。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>最近结束的移动事件；若不存在则返回 null。</returns>
        private MoveEvent FindPreviousMoveEvent(float beat)
        {
            return MoveEvents.LastOrDefault(ev => beat > ev.EndBeat);
        }

        /// <summary>
        /// 解析事件插值起点值：比较最近历史 Event 与前置 Frame，选择更接近目标拍点的一方。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <param name="previousFrame">最近的前置帧。</param>
        /// <returns>事件插值的起始 (X, Y) 坐标。</returns>
        private (float, float) ResolveMoveEventStartValue(float beat, MoveFrame previousFrame)
        {
            var previousEvent = FindPreviousMoveEvent(beat);
            if (IsEventCloserThanFrame(previousEvent, previousFrame))
                return (previousEvent.EndXValue, previousEvent.EndYValue);

            if (previousFrame != null)
                return (previousFrame.XValue, previousFrame.YValue);

            return (0, 0);
        }

        /// <summary>
        /// 判断历史事件是否比前置帧更接近目标拍点。
        /// </summary>
        /// <param name="previousEvent">最近结束的历史事件。</param>
        /// <param name="previousFrame">最近的前置帧。</param>
        /// <returns>若应优先使用历史事件值则为 true，否则为 false。</returns>
        private static bool IsEventCloserThanFrame(MoveEvent previousEvent, MoveFrame previousFrame)
        {
            return previousEvent != null && (previousFrame == null || previousEvent.EndBeat > previousFrame.Beat);
        }

        public JudgeLine Clone()
        {
            return new JudgeLine
            {
                SpeedFrames = SpeedFrames.Select(f => f.Clone()).ToList(),
                MoveFrames = MoveFrames.Select(f => f.Clone()).ToList(),
                RotateFrames = RotateFrames.Select(f => f.Clone()).ToList(),
                AlphaFrames = AlphaFrames.Select(f => f.Clone()).ToList(),
                AlphaEvents = AlphaEvents.Select(e => e.Clone()).ToList(),
                MoveEvents = MoveEvents.Select(e => e.Clone()).ToList(),
                RotateEvents = RotateEvents.Select(e => e.Clone()).ToList(),
                NoteList = NoteList.Select(n => n.Clone()).ToList()
            };
        }
    }
}