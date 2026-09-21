namespace KaedePhi.Core.Intermediate.Model.Controls
{
    /// <summary>
    /// 控制点基类。
    /// </summary>
    public abstract class ControlBase
    {
        /// <summary>
        /// 缓动类型。
        /// </summary>
        public Easing Easing { get; set; } = new(1);

        /// <summary>
        /// X 坐标位置。
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
        public abstract ControlBase Clone();
    }
}
