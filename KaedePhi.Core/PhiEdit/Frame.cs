using System;
using System.Globalization;

namespace KaedePhi.Core.PhiEdit
{
    /// <summary>
    /// 帧（瞬时事件），描述某一拍点的标量值。
    /// 已由引用类型改为结构体，降低谱面处理时的内存开销与 GC 压力；
    /// 旧的 <see cref="LegacyFrame"/> 类仍保留兼容，可通过隐式转换自动映射到本结构体。
    /// </summary>
    public readonly struct Frame
    {
        /// <summary>
        /// 帧所在拍。
        /// </summary>
        public float Beat { get; init; }

        /// <summary>
        /// 帧数值。
        /// </summary>
        public float Value { get; init; }

        /// <summary>
        /// 创建帧。
        /// </summary>
        /// <param name="beat">帧所在拍</param>
        /// <param name="value">帧数值</param>
        public Frame(float beat, float value)
        {
            Beat = beat;
            Value = value;
        }

        /// <summary>
        /// 兼容方法：返回当前帧的副本（结构体按值复制即完成浅拷贝）。
        /// </summary>
        /// <returns>帧副本</returns>
        public Frame Clone() => this;

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int, string)"/>
        /// </summary>
        public override string ToString() => $"Frame(Beat={Beat}, Value={Value})";

        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <param name="head">格式头</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex, string head)
        {
            return head is "cp" or "cm"
                ? throw new ArgumentException(
                    "请使用 MoveFrame 或 MoveEvent 的 ToString 方法，这不是一个 MoveFrame 或 MoveEvent"
                )
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2} {3}",
                    head,
                    judgeLineIndex,
                    Beat,
                    Value
                );
        }

#pragma warning disable CS0618
        /// <summary>
        /// 将旧版帧类自动映射为结构体。
        /// </summary>
        /// <param name="legacy">旧版帧实例</param>
        /// <returns>映射后的结构体</returns>
        public static implicit operator Frame(LegacyFrame legacy) => new(legacy.Beat, legacy.Value);

        /// <summary>
        /// 将结构体映射回旧版帧类（仅供旧接口调用）。
        /// </summary>
        /// <param name="frame">结构体帧</param>
        /// <returns>旧版帧实例</returns>
        public static implicit operator LegacyFrame(Frame frame) =>
            new() { Beat = frame.Beat, Value = frame.Value };
#pragma warning restore CS0618
    }
}
