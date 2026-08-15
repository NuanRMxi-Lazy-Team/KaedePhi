using System;
using System.Globalization;

namespace KaedePhi.Core.PhiEdit
{
    /// <summary>
    /// 旧版帧类型（引用类型实现），仅保留用于兼容迁移期。
    /// 请改用 <see cref="Frame"/> 结构体以降低内存开销；
    /// 本类型实例可通过隐式转换自动映射到 <see cref="Frame"/>，将在后续版本一次性移除。
    /// </summary>
    [Obsolete(
        "Frame 已改为结构体以节省内存开销，请改用 Frame 结构体。"
            + "旧类型仍可通过隐式转换自动映射，本类型将在后续版本移除。"
    )]
    public sealed class LegacyFrame
    {
        /// <summary>
        /// 帧所在拍。
        /// </summary>
        public float Beat { get; set; }

        /// <summary>
        /// 帧数值。
        /// </summary>
        public float Value { get; set; }

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

        /// <summary>
        /// 深拷贝旧版帧。
        /// </summary>
        /// <returns>帧副本</returns>
        public LegacyFrame Clone() => new() { Beat = Beat, Value = Value };
    }
}
