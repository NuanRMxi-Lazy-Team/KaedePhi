namespace KaedePhi.Core.Formats.PhiZonePlayerExtended.JudgeLines
{
    /// <summary>
    /// 用于决定判定线的ScaleX事件对判定线上音符的影响
    /// </summary>
    public enum ScaleOnNotes
    {
        /// <summary>
        /// 不影响
        /// </summary>
        None = 0,

        /// <summary>
        /// 跟随缩放
        /// </summary>
        Scale = 1,

        /// <summary>
        /// 裁剪
        /// </summary>
        Clip = 2,
    }
}
