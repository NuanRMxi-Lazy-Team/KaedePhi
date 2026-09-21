using System.Collections.Generic;
using System.Linq;
using KaedePhi.Core.Primitives;

namespace KaedePhi.Core.Intermediate.Model.Events
{
    /// <summary>
    /// 事件层，包含判定线的所有事件通道。
    /// </summary>
    public class EventLayer
    {
        /// <summary>
        /// X 轴移动事件列表。
        /// </summary>
        public List<Event<double>>? MoveXEvents { get; set; }

        /// <summary>
        /// Y 轴移动事件列表。
        /// </summary>
        public List<Event<double>>? MoveYEvents { get; set; }

        /// <summary>
        /// 旋转事件列表。
        /// </summary>
        public List<Event<double>>? RotateEvents { get; set; }

        /// <summary>
        /// 不透明度事件列表。
        /// </summary>
        public List<Event<int>>? AlphaEvents { get; set; }

        /// <summary>
        /// 速度事件列表。
        /// </summary>
        public List<Event<float>>? SpeedEvents { get; set; }

        /// <summary>
        /// 获取某个拍时，指定事件层级指定事件列表的数值。
        /// <para>
        /// 重叠规则（后来者至上）：<br/>
        /// • 部分重叠 —— 后事件 B 从其 StartBeat 起截断前事件 A；<br/>
        /// • 完全包含 —— A 播放至 B.StartBeat 后切换到 B，B 结束后 A 剩余部分完全忽略；<br/>
        /// • 同起始拍 —— index 靠后者生效，靠前者被忽略。
        /// </para>
        /// </summary>
        /// <param name="events">已按 StartBeat 升序排列的事件列表</param>
        /// <param name="beat">指定拍</param>
        /// <returns>在指定拍时，指定事件列表的数值</returns>
        public static T? GetValueAtBeat<T>(List<Event<T>> events, Beat beat)
            where T : notnull
        {
            // 二分查找：定位 StartBeat <= beat 中 index 最大者（主导事件）
            // 同 StartBeat 时取靠后者（index 更大），满足同起始拍 index 至上规则
            int lo = 0,
                hi = events.Count - 1,
                idx = -1;
            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                if (events[mid].StartBeat <= beat)
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
                return default;

            var dominant = events[idx];

            // 主导事件仍活跃 → 插值
            if (beat <= dominant.EndBeat)
                return dominant.GetValueAtBeat(beat);

            // 主导事件已结束 → 保持其终值，不降级为更早的事件
            // （规则 2：B 结束后 A 的剩余部分完全忽略）
            return dominant.EndValue;
        }

        /// <summary>
        /// 按事件的开始时间稳定排序所有事件。
        /// 使用稳定排序（LINQ OrderBy）以保证同 StartBeat 的事件保持原始 index 顺序，
        /// 从而确保 GetValueAtBeat 中 index 靠后者能正确胜出。
        /// </summary>
        public void Sort()
        {
            MoveXEvents = MoveXEvents?.OrderBy(e => e.StartBeat).ToList();
            MoveYEvents = MoveYEvents?.OrderBy(e => e.StartBeat).ToList();
            RotateEvents = RotateEvents?.OrderBy(e => e.StartBeat).ToList();
            AlphaEvents = AlphaEvents?.OrderBy(e => e.StartBeat).ToList();
            SpeedEvents = SpeedEvents?.OrderBy(e => e.StartBeat).ToList();
        }

        /// <summary>
        /// 克隆，使用深拷贝
        /// </summary>
        /// <returns>一个从里到外都新新的事件层级！</returns>
        public EventLayer Clone()
        {
            // 深拷贝，包括Event列表
            var clone = new EventLayer();
            // 保证列表中的元素也被深拷贝（通过LINQ调用Event的Clone方法）
            if (MoveXEvents is not null)
                clone.MoveXEvents = MoveXEvents.ConvertAll(e => e.Clone());
            if (MoveYEvents is not null)
                clone.MoveYEvents = MoveYEvents.ConvertAll(e => e.Clone());
            if (RotateEvents is not null)
                clone.RotateEvents = RotateEvents.ConvertAll(e => e.Clone());
            if (AlphaEvents is not null)
                clone.AlphaEvents = AlphaEvents.ConvertAll(e => e.Clone());
            if (SpeedEvents is not null)
                clone.SpeedEvents = SpeedEvents.ConvertAll(e => e.Clone());
            return clone;
        }

        /// <summary>
        /// 强行预期化，将空列表设置为null，保证Json序列化时不包含空列表
        /// </summary>
        public void Anticipation()
        {
            if (MoveXEvents is { Count: 0 })
                MoveXEvents = null;
            if (MoveYEvents is { Count: 0 })
                MoveYEvents = null;
            if (RotateEvents is { Count: 0 })
                RotateEvents = null;
            if (AlphaEvents is { Count: 0 })
                AlphaEvents = null;
            if (SpeedEvents is { Count: 0 })
                SpeedEvents = null;
        }
    }
}
