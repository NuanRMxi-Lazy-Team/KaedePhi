using System;
using System.Collections.Generic;
using System.Linq;

namespace KaedePhi.Core.PhiEdit
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
        /// 优先级：精确匹配 Frame -> 当前生效 Event 插值 -> 最近的历史主导 Event/Frame -> 默认 (0, 0)。
        /// <para>
        /// 重叠规则（后来者至上）：<br/>
        /// • 部分重叠 —— 后事件 B 从其 StartBeat 起截断前事件 A；<br/>
        /// • 完全包含 —— A 播放至 B.StartBeat 后切换到 B，B 结束后 A 剩余部分完全忽略；<br/>
        /// • 同起始拍 —— index 靠后者生效，靠前者被忽略。
        /// </para>
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
                var (startX, startY) = ResolveMoveEventStartValue(activeEvent, previousFrame);
                return activeEvent.GetValueAtBeat(beat, startX, startY);
            }

            var previousEvent = FindPreviousMoveEvent(beat);
            if (previousEvent is null)
                return (0, 0);
            if (IsEventCloserThanFrame(previousEvent, previousFrame))
                return (previousEvent.EndXValue, previousEvent.EndYValue);

            if (previousFrame is not null)
                return (previousFrame.Value.XValue, previousFrame.Value.YValue);

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
        private (
            bool foundExactFrame,
            (float, float) value,
            MoveFrame? previousFrame
        ) FindExactOrPreviousFrame(float beat)
        {
            // 二分查找：定位 Beat <= beat 中 index 最大者（最近前置帧或精确帧）
            int lo = 0,
                hi = MoveFrames.Count - 1,
                idx = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (MoveFrames[mid].Beat <= beat)
                {
                    idx = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (idx < 0)
                return (false, default, null);

            var frame = MoveFrames[idx];
            if (Math.Abs(frame.Beat - beat) < 0.0001f)
                return (true, (frame.XValue, frame.YValue), frame);

            return (false, default, frame);
        }

        /// <summary>
        /// 查找目标拍点上正在生效的主导移动事件（后来者至上）。
        /// <para>
        /// 主导事件定义：MoveEvents 中 StartBeat &lt;= beat 的所有事件里 index 最大者（同 StartBeat 取靠后者）。
        /// 若主导事件已结束（EndBeat &lt; beat），不降级为早先事件，直接返回 null。
        /// </para>
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>当前主导且仍活跃的移动事件；若主导事件已结束或无任何事件则为 null。</returns>
        private MoveEvent? FindActiveMoveEvent(float beat)
        {
            // 二分查找：定位 StartBeat <= beat + ε 中 index 最大者（主导事件）
            // 同 StartBeat 时取靠后者（index 更大），满足同起始拍 index 至上规则
            int lo = 0,
                hi = MoveEvents.Count - 1,
                idx = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (MoveEvents[mid].StartBeat <= beat + 0.0001f)
                {
                    idx = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (idx < 0)
                return null;

            var dominant = MoveEvents[idx];

            // 主导事件已结束时不降级——A 被 B 截断后，A 剩余部分完全忽略
            return beat <= dominant.EndBeat + 0.0001f ? dominant : null;
        }

        /// <summary>
        /// 查找目标拍点对应的主导已结束移动事件，用于持续保持其终值。
        /// 等价于：StartBeat &lt;= beat 中 index 最大且已结束（EndBeat &lt; beat）的事件。
        /// </summary>
        /// <param name="beat">目标拍点。</param>
        /// <returns>主导已结束移动事件；若不存在则返回 null。</returns>
        private MoveEvent? FindPreviousMoveEvent(float beat)
        {
            // 二分查找主导事件（StartBeat <= beat 中 index 最大者），
            // 然后向前扫描首个已结束事件——即主导已结束事件。
            // 由于列表按 StartBeat 升序，一旦遇到已结束事件，其前方所有事件必然也已结束，
            // 因此首个已结束事件即为 index 最大的已结束事件。
            int lo = 0,
                hi = MoveEvents.Count - 1,
                idx = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (MoveEvents[mid].StartBeat <= beat + 0.0001f)
                {
                    idx = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            for (int i = idx; i >= 0; i--)
            {
                if (beat > MoveEvents[i].EndBeat)
                    return MoveEvents[i];
            }

            return null;
        }

        /// <summary>
        /// 计算 activeEvent 的插值起点：activeEvent.StartBeat 处的实际生效值。
        /// <para>
        /// 若 activeEvent.StartBeat 处有精确帧，帧值优先；<br/>
        /// 否则查找前驱主导事件（列表中 index 更小且 StartBeat &lt;= activeEvent.StartBeat 的最后一个）：<br/>
        /// • 前驱事件与 activeEvent 重叠（EndBeat &gt;= activeEvent.StartBeat）→ 递归计算前驱在该拍点的值；<br/>
        /// • 前驱事件已结束 → 直接取其 EndValue；<br/>
        /// • 无前驱事件 → 使用前置帧或 (0, 0)。
        /// </para>
        /// </summary>
        /// <param name="activeEvent">当前主导且活跃的事件。</param>
        /// <param name="hintPreviousFrame">查询拍点前最近的帧（用于无前驱事件时的兜底）。</param>
        /// <returns>activeEvent 插值起点的 (X, Y) 坐标。</returns>
        private (float, float) ResolveMoveEventStartValue(
            MoveEvent activeEvent,
            MoveFrame? hintPreviousFrame
        )
        {
            // 优先：activeEvent.StartBeat 处有精确帧
            var frameAtStart = FindMoveFrameExactAt(activeEvent.StartBeat);
            if (frameAtStart != null)
                return (frameAtStart.Value.XValue, frameAtStart.Value.YValue);

            // 找前驱主导事件
            var preceding = FindPrecedingDominantMoveEvent(activeEvent);
            if (preceding != null)
            {
                if (preceding.EndBeat >= activeEvent.StartBeat - 0.0001f)
                {
                    // 前驱事件在 activeEvent 开始时仍活跃（重叠）→ 递归计算其在该拍点的值
                    var (innerX, innerY) = ResolveMoveEventStartValue(preceding, hintPreviousFrame);
                    return preceding.GetValueAtBeat(activeEvent.StartBeat, innerX, innerY);
                }

                // 前驱事件已结束，直接使用其终值
                return (preceding.EndXValue, preceding.EndYValue);
            }

            // 无前驱事件，使用 hintPreviousFrame（它一定在 activeEvent.StartBeat 之前或恰好处）
            if (hintPreviousFrame != null)
                return (hintPreviousFrame.Value.XValue, hintPreviousFrame.Value.YValue);

            return (0, 0);
        }

        /// <summary>
        /// 判断历史事件是否比前置帧更接近目标拍点。
        /// </summary>
        /// <param name="previousEvent">最近结束的历史事件。</param>
        /// <param name="previousFrame">最近的前置帧。</param>
        /// <returns>若应优先使用历史事件值则为 true，否则为 false。</returns>
        private static bool IsEventCloserThanFrame(
            MoveEvent? previousEvent,
            MoveFrame? previousFrame
        )
        {
            return previousEvent != null
                && (previousFrame == null || previousEvent.EndBeat > previousFrame.Value.Beat);
        }

        /// <summary>
        /// 在 MoveEvents 中查找 activeEvent 的前驱主导事件：
        /// 列表中位于 activeEvent 之前（index 更小）且 StartBeat &lt;= activeEvent.StartBeat 的最后一个事件。
        /// </summary>
        private MoveEvent? FindPrecedingDominantMoveEvent(MoveEvent activeEvent)
        {
            // 列表按 StartBeat 升序，因此 activeEvent 的前驱（index - 1）必然满足
            // StartBeat <= activeEvent.StartBeat，无需线性扫描。
            int idx = MoveEvents.IndexOf(activeEvent);
            return idx > 0 ? MoveEvents[idx - 1] : null;
        }

        /// <summary>
        /// 在 MoveFrames 中查找精确位于给定拍点的帧（误差 &lt; 0.0001）。
        /// </summary>
        private MoveFrame? FindMoveFrameExactAt(float beat)
        {
            // 二分查找：定位 Beat <= beat 中 index 最大者，再检验是否精确命中
            int lo = 0,
                hi = MoveFrames.Count - 1,
                idx = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (MoveFrames[mid].Beat <= beat + 0.0001f)
                {
                    idx = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            if (idx >= 0 && Math.Abs(MoveFrames[idx].Beat - beat) < 0.0001f)
                return MoveFrames[idx];

            return null;
        }

        /// <summary>
        /// 按时间对所有帧列表和事件列表进行稳定排序。
        /// <para>
        /// 使用稳定排序（LINQ OrderBy）以保证同 Beat/StartBeat 的条目保持原始 index 顺序，
        /// 从而确保 GetMoveAtBeat 等查询方法中 index 靠后者能正确胜出（后来者至上规则）。
        /// </para>
        /// </summary>
        public void Sort()
        {
            SpeedFrames = SpeedFrames.OrderBy(f => f.Beat).ToList();
            MoveFrames = MoveFrames.OrderBy(f => f.Beat).ToList();
            RotateFrames = RotateFrames.OrderBy(f => f.Beat).ToList();
            AlphaFrames = AlphaFrames.OrderBy(f => f.Beat).ToList();
            AlphaEvents = AlphaEvents.OrderBy(e => e.StartBeat).ToList();
            MoveEvents = MoveEvents.OrderBy(e => e.StartBeat).ToList();
            RotateEvents = RotateEvents.OrderBy(e => e.StartBeat).ToList();
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
                NoteList = NoteList.Select(n => n.Clone()).ToList(),
            };
        }
    }
}
