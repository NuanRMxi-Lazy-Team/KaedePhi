using System.Linq;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.KaedePhi
{
    public class Note
    {
        private Beat _endBeat = new(new[] { 1, 0, 1 });
        private bool _hasExplicitEndBeat;

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
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 });

        /// <summary>
        /// 模拟器保留字段
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// 音符的结束拍
        /// </summary>
        public Beat EndBeat
        {
            get => _endBeat;
            set
            {
                _endBeat = value;
                _hasExplicitEndBeat = true;
            }
        }

        [JsonIgnore]
        internal bool HasExplicitEndBeat => _hasExplicitEndBeat;

        /// <summary>
        /// 模拟器保留字段
        /// </summary>
        public float EndTime { get; set; }

        /// <summary>
        /// 音符是否为假音符
        /// </summary>
        public bool IsFake { get; set; }

        /// <summary>
        /// 音符相对于判定线的X坐标
        /// </summary>
        public double PositionX { get; set; }

        /// <summary>
        /// 音符宽度倍率
        /// </summary>
        public float WidthRatio { get; set; } = 1.0f;

        /// <summary>
        /// 音符判定宽度倍率
        /// </summary>
        public float JudgeArea { get; set; } = 1.0f;

        /// <summary>
        /// 音符下落速度倍率
        /// </summary>
        public float SpeedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// 音符类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）
        /// </summary>
        public NoteType Type { get; set; } = NoteType.Tap;

        /// <summary>
        /// 音符可见时间，单位为秒
        /// </summary>
        public float VisibleTime { get; set; } = 999999.0000f;

        /// <summary>
        /// 音符相对于判定线的Y轴偏移
        /// </summary>
        public double YOffset { get; set; }

        /// <summary>
        /// 音符颜色（RGB，顶点颜色乘法）
        /// </summary>
        public byte[] Tint { get; set; } = { 255, 255, 255 };

        /// <summary>
        /// 打击特效颜色（RGB，顶点颜色乘法）
        /// </summary>
        public byte[]? HitFxColor { get; set; }

        /// <summary>
        /// 音符打击音效相对路径
        /// </summary>
        public string? HitSound { get; set; }

        /// <summary>
        /// 保留字段
        /// </summary>
        public float FloorPosition { get; set; }

        /// <summary>
        /// Hold音符结束时的FloorPosition (用于Hold渲染)
        /// </summary>
        public float EndFloorPosition { get; set; }

        /// <summary>
        /// 深拷贝音符。
        /// </summary>
        /// <returns>音符副本</returns>
        public Note Clone()
        {
            // 有Beat，不能使用MemberwiseClone
            return new Note
            {
                Above = Above,
                Alpha = Alpha,
                StartBeat = new Beat((int[])StartBeat),
                StartTime = StartTime,
                EndBeat = new Beat((int[])EndBeat),
                EndTime = EndTime,
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
                HitSound = HitSound,
                FloorPosition = FloorPosition,
                EndFloorPosition = EndFloorPosition,
                _hasExplicitEndBeat = _hasExplicitEndBeat,
            };
        }
    }
}
