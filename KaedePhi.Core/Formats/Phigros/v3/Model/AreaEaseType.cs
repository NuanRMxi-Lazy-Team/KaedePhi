namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 方块区域事件使用的缓动类型。
    /// </summary>
    public enum AreaEaseType
    {
        /// <summary>
        /// 线性缓动。
        /// </summary>
        Linear = 0,

        /// <summary>
        /// 二次缓入。
        /// </summary>
        EaseInQuad = 1,

        /// <summary>
        /// 二次缓出。
        /// </summary>
        EaseOutQuad = 2,

        /// <summary>
        /// 二次缓入缓出。
        /// </summary>
        EaseInOutQuad = 3,

        /// <summary>
        /// 三次缓入。
        /// </summary>
        EaseInCubic = 4,

        /// <summary>
        /// 三次缓出。
        /// </summary>
        EaseOutCubic = 5,

        /// <summary>
        /// 三次缓入缓出。
        /// </summary>
        EaseInOutCubic = 6,

        /// <summary>
        /// 四次缓入。
        /// </summary>
        EaseInQuart = 7,

        /// <summary>
        /// 四次缓出。
        /// </summary>
        EaseOutQuart = 8,

        /// <summary>
        /// 四次缓入缓出。
        /// </summary>
        EaseInOutQuart = 9,

        /// <summary>
        /// 五次缓入。
        /// </summary>
        EaseInQuint = 10,

        /// <summary>
        /// 五次缓出。
        /// </summary>
        EaseOutQuint = 11,

        /// <summary>
        /// 五次缓入缓出。
        /// </summary>
        EaseInOutQuint = 12,

        /// <summary>
        /// 缓动值恒为零。
        /// </summary>
        Zero = 13,

        /// <summary>
        /// 缓动值恒为一。
        /// </summary>
        One = 14,
    }
}