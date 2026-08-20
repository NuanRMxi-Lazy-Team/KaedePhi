using System.Linq;
using System.Runtime.Serialization;
using KaedePhi.Core.Common;
using KaedePhi.Core.RePhiEdit.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.RePhiEdit
{
    public class Note
    {
        private Beat _endBeat = new(new[] { 1, 0, 1 });
        private bool _hasExplicitEndBeat;

        /// <summary>
        /// 音符是否在判定线上方下落，true为上方，false为下方
        /// </summary>
        [JsonProperty("above")]
        [JsonConverter(typeof(BoolConverter))]
        public bool Above { get; set; } = true;

        /// <summary>
        /// 音符的不透明度
        /// </summary>
        [JsonProperty("alpha")]
        public byte Alpha { get; set; } = 255;

        /// <summary>
        /// 音符的起始拍
        /// </summary>
        [JsonProperty("startTime")]
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 }); // 开始时间

        /// <summary>
        /// 音符的结束拍
        /// </summary>
        [JsonProperty("endTime")]
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
        /// 音符是否为假音符
        /// </summary>
        [JsonProperty("isFake")]
        [JsonConverter(typeof(BoolConverter))]
        public bool IsFake { get; set; }

        /// <summary>
        /// 音符相对于判定线的X坐标
        /// </summary>
        [JsonProperty("positionX")]
        public float PositionX { get; set; } // X坐标

        /// <summary>
        /// 音符宽度倍率
        /// </summary>
        [JsonProperty("size")]
        public float Size { get; set; } = 1.0f; // 宽度倍率

        /// <summary>
        /// 音符判定宽度倍率
        /// </summary>
        [JsonProperty("judgeArea")]
        public float JudgeArea { get; set; } = 1.0f;

        /// <summary>
        /// 音符下落速度倍率
        /// </summary>
        [JsonProperty("speed")]
        public float SpeedMultiplier { get; set; } = 1.0f; // 速度倍率

        /// <summary>
        /// 音符类型
        /// </summary>
        [JsonProperty("type")]
        [JsonConverter(typeof(NoteTypeConverter))]
        public NoteType Type { get; set; } = NoteType.Tap; // 类型（1 为 Tap、2 为 Hold、3 为 Flick、4 为 Drag）

        /// <summary>
        /// 音符可见时间，单位为秒
        /// </summary>
        [JsonProperty("visibleTime")]
        public float VisibleTime { get; set; } = 999999.0000f; // 可见时间（单位为秒）

        /// <summary>
        /// 音符相对于判定线的Y轴偏移
        /// </summary>
        [JsonProperty("yOffset")]
        public float YOffset { get; set; } // Y偏移

        /// <summary>
        /// 音符颜色（RGB，顶点颜色乘法），此字段在Json中优先为tint，早期版本使用过color字段
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        [JsonProperty("tint")]
        public byte[] Color { get; set; } = { 255, 255, 255 }; // 颜色（RGB）

        [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ColorConverter))]
        private byte[]? ColorLegacyField
        {
            get => null; // 序列化时不输出
            set
            {
                if (value != null)
                    Color = value;
            } // 反序列化时赋值
        }

        /// <summary>
        /// 打击特效颜色（RGB，顶点颜色乘法）
        /// </summary>
        [JsonProperty("tintHitEffects", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ColorConverter))]
        public byte[]? HitFxColor { get; set; }

        /// <summary>
        /// 音符打击音效相对路径
        /// </summary>
        [JsonProperty("hitsound", NullValueHandling = NullValueHandling.Ignore)]
        public string? HitSound { get; set; } // 音效

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context) => _hasExplicitEndBeat = false;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Type == NoteType.Hold && (!_hasExplicitEndBeat || EndBeat <= StartBeat))
                throw new JsonSerializationException("Hold 音符缺少有效的结束拍。");
        }

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
                Size = Size,
                JudgeArea = JudgeArea,
                SpeedMultiplier = SpeedMultiplier,
                Type = Type,
                VisibleTime = VisibleTime,
                YOffset = YOffset,
                Color = Color.ToArray(),
                HitFxColor = HitFxColor?.ToArray(),
                HitSound = HitSound,
                _hasExplicitEndBeat = _hasExplicitEndBeat,
            };
        }
    }
}
