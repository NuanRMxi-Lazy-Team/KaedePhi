using System.ComponentModel;

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
        /// Phigros 原版对线性缓动使用的名称。
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Liner = Linear,

        /// <summary>
        /// 二次缓入。
        /// </summary>
        InSine = 1,

        /// <summary>
        /// 二次缓出。
        /// </summary>
        OutSine = 2,

        /// <summary>
        /// 二次缓入缓出。
        /// </summary>
        InOutSine = 3,

        /// <summary>
        /// 三次缓入。
        /// </summary>
        InQuad = 4,

        /// <summary>
        /// 三次缓出。
        /// </summary>
        OutQuad = 5,

        /// <summary>
        /// 三次缓入缓出。
        /// </summary>
        InOutQuad = 6,

        /// <summary>
        /// 四次缓入。
        /// </summary>
        InCubic = 7,

        /// <summary>
        /// 四次缓出。
        /// </summary>
        OutCubic = 8,

        /// <summary>
        /// 四次缓入缓出。
        /// </summary>
        InOutCubic = 9,

        /// <summary>
        /// 五次缓入。
        /// </summary>
        InQuart = 10,

        /// <summary>
        /// 五次缓出。
        /// </summary>
        OutQuart = 11,

        /// <summary>
        /// 五次缓入缓出。
        /// </summary>
        InOutQuart = 12,

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