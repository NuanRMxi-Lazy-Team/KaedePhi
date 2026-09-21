using System.Collections.Generic;
using System.Linq;
using KaedePhi.Core.Formats.RePhiEdit.Serialization.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model.Events
{
    public class ExtendLayer
    {
        /// <summary>
        /// 判定线纹理颜色事件列表，颜色格式为RGB字节数组，使用顶点颜色乘法
        /// </summary>
        [JsonProperty(
            "colorEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        [JsonConverter(typeof(ColorEventsConverter))]
        public List<Event<byte[]>>? ColorEvents { get; set; }

        /// <summary>
        /// 判定线纹理宽度缩放事件列表
        /// </summary>
        [JsonProperty(
            "scaleXEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        public List<Event<float>>? ScaleXEvents { get; set; }

        /// <summary>
        /// 判定线纹理高度缩放事件列表
        /// </summary>
        [JsonProperty(
            "scaleYEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        public List<Event<float>>? ScaleYEvents { get; set; }

        /// <summary>
        /// 判定线文字纹理事件列表
        /// </summary>
        [JsonProperty(
            "textEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        public List<Event<string>>? TextEvents { get; set; }

        /// <summary>
        /// 画笔事件列表，值为画笔大小
        /// </summary>
        [JsonProperty(
            "paintEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        public List<Event<float>>? PaintEvents { get; set; }

        /// <summary>
        /// 判定线动图播放进度事件列表，值为动图帧进度（0~1之间）
        /// </summary>
        [JsonProperty(
            "gifEvents",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        )]
        public List<Event<float>>? GifEvents { get; set; }

        /// <summary>
        /// 判定线倾斜事件列表，值为Z轴倾斜角度，顺时针为正
        /// </summary>
        [JsonProperty("inclineEvents", NullValueHandling = NullValueHandling.Ignore)]
        public List<Event<float>>? InclineEvents { get; set; }

        /// <summary>
        /// 按事件的开始时间稳定排序所有事件。
        /// </summary>
        public void Sort()
        {
            ColorEvents = ColorEvents?.OrderBy(e => e.StartBeat).ToList();
            ScaleXEvents = ScaleXEvents?.OrderBy(e => e.StartBeat).ToList();
            ScaleYEvents = ScaleYEvents?.OrderBy(e => e.StartBeat).ToList();
            TextEvents = TextEvents?.OrderBy(e => e.StartBeat).ToList();
            PaintEvents = PaintEvents?.OrderBy(e => e.StartBeat).ToList();
            GifEvents = GifEvents?.OrderBy(e => e.StartBeat).ToList();
            InclineEvents = InclineEvents?.OrderBy(e => e.StartBeat).ToList();
        }

        /// <summary>
        /// 深克隆当前 ExtendLayer 对象
        /// </summary>
        public ExtendLayer Clone()
        {
            // 深拷贝，包括Event列表
            var clone = new ExtendLayer();
            // 保证列表中的元素也被深拷贝（通过LINQ调用Event的Clone方法）
            if (ColorEvents is not null)
                clone.ColorEvents = ColorEvents.ConvertAll(e => e.Clone());
            if (ScaleXEvents is not null)
                clone.ScaleXEvents = ScaleXEvents.ConvertAll(e => e.Clone());
            if (ScaleYEvents is not null)
                clone.ScaleYEvents = ScaleYEvents.ConvertAll(e => e.Clone());
            if (TextEvents is not null)
                clone.TextEvents = TextEvents.ConvertAll(e => e.Clone());
            if (PaintEvents is not null)
                clone.PaintEvents = PaintEvents.ConvertAll(e => e.Clone());
            if (GifEvents is not null)
                clone.GifEvents = GifEvents.ConvertAll(e => e.Clone());
            if (InclineEvents is not null)
                clone.InclineEvents = InclineEvents.ConvertAll(e => e.Clone());
            return clone;
        }

        /// <summary>
        /// 强行预期化，让层级有与RePhiEdit几乎一致的"出色表现"，兼容按照现行行为的模拟器。
        /// </summary>
        public void Anticipation()
        {
            Sort();
            if (ColorEvents is { Count: 0 })
                ColorEvents = null;
            if (ScaleXEvents is { Count: 0 })
                ScaleXEvents = null;
            if (ScaleYEvents is { Count: 0 })
                ScaleYEvents = null;
            if (TextEvents is { Count: 0 })
                TextEvents = null;
            if (PaintEvents is { Count: 0 })
                PaintEvents = null;
            if (GifEvents is { Count: 0 })
                GifEvents = null;
            // 倾斜事件的缓动数值，按照rpe规范，其初始数值为判定线索引，但是我实在没办法接受这个如此抽象的默认值，因此这里直接给线性缓动，不做特殊处理
            // 倾斜事件早废弃了，我写在这里就是单纯的防止某些模拟器抽风
            // 所以，为什么，EasingType可以为0，可以大于30，cmdysj！RePhiEdit！Fxxk！
            if (InclineEvents is null || InclineEvents.Count == 0)
                InclineEvents = new List<Event<float>> { new() };
        }
    }
}
