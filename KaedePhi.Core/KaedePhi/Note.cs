using System;
using System.Linq;
using KaedePhi.Core.Common;

// STJ 特性均使用完全限定名，避免命名冲突

namespace KaedePhi.Core.KaedePhi
{
    public class Note
    {
        /// <summary>
        /// 音符是否在判定线上方下落，true为上方，false为下方
        /// </summary>
        public bool Above { get; set; } = true;

        /// <summary>
        /// 音符的不透明度
        /// </summary>
        public byte Alpha { get; set; } = 255;

        /// <summary>
        /// 音符的起始拍
        /// </summary>
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 }); // 开始时间

        /// <summary>
        /// 音符的结束拍
        /// </summary>
        public Beat EndBeat { get; set; } = new(new[] { 1, 0, 1 }); // 结束时间

        /// <summary>
        /// 音符是否为假音符
        /// </summary>
        public bool IsFake { get; set; } = false;

        /// <summary>
        /// 音符相对于判定线的X坐标
        /// </summary>
        public double PositionX { get; set; } = 0.0f; // X坐标

        /// <summary>
        /// 音符宽度倍率
        /// </summary>
        [Obsolete("想出用Size代表Width的已经很神人了，我不应该忘记删这里，总之，请改用WidthRatio。",true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public float Size
        {
            get => WidthRatio;
            set => WidthRatio = value;
        } // 宽度倍率

        /// <summary>
        /// 音符宽度倍率（正式命名）
        /// </summary>
        public float WidthRatio { get; set; } = 1.0f;

        /// <summary>
        /// 音符判定宽度倍率
        /// </summary>
        public float JudgeArea { get; set; } = 1.0f;

        /// <summary>
        /// 音符下落速度倍率
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1.0f; // 速度倍率

        /// <summary>
        /// 音符类型
        /// </summary>
        public NoteType Type { get; set; } = NoteType.Tap; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）

        /// <summary>
        /// 音符可见时间，单位为秒
        /// </summary>
        public float VisibleTime { get; set; } = 999999.0000f; // 可见时间（单位为秒）

        /// <summary>
        /// 音符相对于判定线的Y轴偏移
        /// </summary>
        public double YOffset { get; set; } = 0.0f; // Y偏移

        /// <summary>
        /// 音符颜色（RGB，顶点颜色乘法）
        /// </summary>
        public byte[] Tint { get; set; } = { 255, 255, 255 }; // 颜色（RGB）

        /// <summary>
        /// 打击特效颜色（RGB，顶点颜色乘法）
        /// </summary>
        public byte[] HitFxColor { get; set; } = null;

        /// <summary>
        /// 音符打击音效相对路径
        /// </summary>
        public string HitSound { get; set; } = null; // 音效

        public Note Clone()
        {
            // 有Beat，不能使用MemberwiseClone
            return new Note
            {
                Above = Above,
                Alpha = Alpha,
                StartBeat = new Beat((int[])StartBeat),
                EndBeat = new Beat((int[])EndBeat),
                IsFake = IsFake,
                PositionX = PositionX,
                WidthRatio = WidthRatio,
                JudgeArea = JudgeArea,
                SpeedMultiplier = SpeedMultiplier,
                Type = Type,
                VisibleTime = VisibleTime,
                YOffset = YOffset,
                Tint = Tint.ToArray(),
                HitFxColor = HitFxColor?.ToArray(),
                HitSound = HitSound
            };
        }
    }

    public enum NoteType
    {
        Tap = 1,
        Hold = 2,
        Flick = 3,
        Drag = 4
    }
}