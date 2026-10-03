namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 方块区域在时间轴上的显示阶段。
    /// </summary>
    public enum BlockAreaPhase
    {
        /// <summary>
        /// 尚未到达显示时间。
        /// </summary>
        HiddenBefore,

        /// <summary>
        /// 显示但未激活。
        /// </summary>
        Disabled,

        /// <summary>
        /// 激活前的预备阶段。
        /// </summary>
        Ready,

        /// <summary>
        /// 已激活。
        /// </summary>
        Active,

        /// <summary>
        /// 已超过显示时间。
        /// </summary>
        HiddenAfter,
    }
}