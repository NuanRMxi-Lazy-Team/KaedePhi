using System;
using System.Globalization;

namespace KaedePhi.Core.PhiEdit
{
    /// <summary>
    /// 旧版移动帧类型（引用类型实现），仅保留用于兼容迁移期。
    /// 请改用 <see cref="MoveFrame"/> 结构体以降低内存开销；
    /// 本类型实例可通过隐式转换自动映射到 <see cref="MoveFrame"/>，将在后续版本一次性移除。
    /// </summary>
    [Obsolete(
        "MoveFrame 已改为结构体以节省内存开销，请改用 MoveFrame 结构体。"
            + "旧类型仍可通过隐式转换自动映射，本类型将在后续版本移除。"
    )]
    public sealed class LegacyMoveFrame
    {
        /// <summary>
        /// 帧所在拍。
        /// </summary>
        public float Beat { get; set; }

        /// <summary>
        /// X 坐标值。
        /// </summary>
        public float XValue { get; set; }

        /// <summary>
        /// Y 坐标值。
        /// </summary>
        public float YValue { get; set; }

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int)"/>
        /// </summary>
        public override string ToString() =>
            $"MoveFrame(Beat={Beat}, XValue={XValue}, YValue={YValue})";

        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} {3} {4}",
                "cp",
                judgeLineIndex,
                Beat,
                XValue,
                YValue
            );

        /// <summary>
        /// 深拷贝旧版移动帧。
        /// </summary>
        /// <returns>移动帧副本</returns>
        public LegacyMoveFrame Clone() => new() { Beat = Beat, XValue = XValue, YValue = YValue };
    }
}
