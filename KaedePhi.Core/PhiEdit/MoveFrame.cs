using System.Globalization;

namespace KaedePhi.Core.PhiEdit
{
    /// <summary>
    /// 移动帧（瞬时事件），描述某一拍点上的判定线位置。
    /// 已由引用类型改为结构体，降低谱面处理时的内存开销与 GC 压力；
    /// 旧的 <see cref="LegacyMoveFrame"/> 类仍保留兼容，可通过隐式转换自动映射到本结构体。
    /// </summary>
    public readonly struct MoveFrame
    {
        /// <summary>
        /// 帧所在拍。
        /// </summary>
        public float Beat { get; init; }

        /// <summary>
        /// X 坐标值。
        /// </summary>
        public float XValue { get; init; }

        /// <summary>
        /// Y 坐标值。
        /// </summary>
        public float YValue { get; init; }

        /// <summary>
        /// 创建移动帧。
        /// </summary>
        /// <param name="beat">帧所在拍</param>
        /// <param name="xValue">X 坐标值</param>
        /// <param name="yValue">Y 坐标值</param>
        public MoveFrame(float beat, float xValue, float yValue)
        {
            Beat = beat;
            XValue = xValue;
            YValue = yValue;
        }

        /// <summary>
        /// 兼容方法：返回当前移动帧的副本（结构体按值复制即完成浅拷贝）。
        /// </summary>
        /// <returns>移动帧副本</returns>
        public MoveFrame Clone() => this;

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

#pragma warning disable CS0618
        /// <summary>
        /// 将旧版移动帧类自动映射为结构体。
        /// </summary>
        /// <param name="legacy">旧版移动帧实例</param>
        /// <returns>映射后的结构体</returns>
        public static implicit operator MoveFrame(LegacyMoveFrame legacy) =>
            new(legacy.Beat, legacy.XValue, legacy.YValue);

        /// <summary>
        /// 将结构体映射回旧版移动帧类（仅供旧接口调用）。
        /// </summary>
        /// <param name="frame">结构体移动帧</param>
        /// <returns>旧版移动帧实例</returns>
        public static implicit operator LegacyMoveFrame(MoveFrame frame) =>
            new()
            {
                Beat = frame.Beat,
                XValue = frame.XValue,
                YValue = frame.YValue,
            };
#pragma warning restore CS0618
    }
}
