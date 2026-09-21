using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Model.Events
{
    /// <summary>
    /// 扩展事件层（故事板），包含判定线的视觉效果事件。
    /// </summary>
    public class ExtendLayer
    {
        /// <summary>
        /// 判定线纹理颜色事件列表，颜色格式为RGB字节数组，使用顶点颜色乘法
        /// </summary>
        public List<Event<byte[]>>? ColorEvents { get; set; }

        /// <summary>
        /// 判定线纹理宽度缩放事件列表
        /// </summary>
        public List<Event<float>>? ScaleXEvents { get; set; }

        /// <summary>
        /// 判定线纹理高度缩放事件列表
        /// </summary>
        public List<Event<float>>? ScaleYEvents { get; set; }

        /// <summary>
        /// 判定线文字纹理事件列表
        /// </summary>
        public List<Event<string>>? TextEvents { get; set; }

        /// <summary>
        /// 画笔事件列表，值为画笔大小
        /// </summary>
        public List<Event<float>>? PaintEvents { get; set; }

        /// <summary>
        /// 判定线动图播放进度事件列表，值为动图帧进度（0~1之间）
        /// </summary>
        public List<Event<float>>? GifEvents { get; set; }

        /// <summary>
        /// 判定线倾斜事件列表，值为Z轴倾斜角度，顺时针为正
        /// </summary>
        public List<Event<float>>? InclineEvents { get; set; }

        /// <summary>
        /// 深拷贝扩展事件层。
        /// </summary>
        /// <returns>扩展事件层副本</returns>
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
    }
}
