namespace KaedePhi.Core.Primitives
{
    /// <summary>
    /// 音符类型
    /// </summary>
    public enum NoteType
    {
        /// <summary>点击音符</summary>
        Tap = 1,

        /// <summary>长按音符</summary>
        Hold = 2,

        /// <summary>滑动音符</summary>
        Flick = 3,

        /// <summary>拖拽音符</summary>
        Drag = 4,
    }
}
