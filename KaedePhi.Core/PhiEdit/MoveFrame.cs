namespace KaedePhi.Core.PhiEdit
{
    public class MoveFrame
    {
        public float Beat { get; set; }
        public float XValue { get; set; }
        public float YValue { get; set; }

        /// <summary>
        /// 调试用方法，不要调用，请改用<see cref="ToString(int)"/>
        /// </summary>
        public override string ToString()
            => $"MoveFrame(Beat={Beat}, XValue={XValue}, YValue={YValue})";

        /// <summary>
        /// 用于将瞬时事件转换为PhiEditor Chart格式的字符串
        /// </summary>
        /// <param name="judgeLineIndex">判定线索引</param>
        /// <returns>PhiEditor Chart格式字符串</returns>
        public string ToString(int judgeLineIndex)
            => $"cp {judgeLineIndex} {Beat} {XValue} {YValue}";

        public MoveFrame Clone()
        {
            return new MoveFrame
            {
                Beat = Beat,
                XValue = XValue,
                YValue = YValue
            };
        }
    }
}