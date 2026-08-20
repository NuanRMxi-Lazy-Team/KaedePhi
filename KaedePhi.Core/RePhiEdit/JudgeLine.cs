using System.Collections.Generic;
using System.Linq;
using KaedePhi.Core.Common;
using KaedePhi.Core.RePhiEdit.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.RePhiEdit
{
    public class JudgeLine
    {
        /// <summary>
        /// 判定线名称
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; } = "KaedePhi_RePhiEditJudgeLine";

        /// <summary>
        /// 判定线纹理相对路径，默认值为line.png
        /// </summary>
        [JsonProperty("Texture")]
        public string Texture { get; set; } = CoreConstants.DefaultTexture;

        /// <summary>
        /// 判定线纹理锚点(0~1之间)，默认值为中心点(0.5, 0.5)
        /// </summary>
        [JsonProperty("anchor")]
        public float[] Anchor { get; set; } = { 0.5f, 0.5f }; // 判定线纹理锚点

        /// <summary>
        /// 判定线事件层列表
        /// </summary>
        [JsonProperty("eventLayers", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Events.EventLayer> EventLayers { get; set; } = new(); // 事件层

        /// <summary>
        /// 父级判定线索引，-1表示无父级
        /// </summary>
        [JsonProperty("father")]
        public int Father { get; set; } = -1; // 父级

        /// <summary>
        /// 是否遮罩越过判定线的音符（已被打击的除外）
        /// </summary>
        [JsonProperty("isCover")]
        [JsonConverter(typeof(BoolConverter))]
        public bool IsCover { get; set; } = true; // 是否遮罩

        /// <summary>
        /// 判定线音符列表
        /// </summary>
        [JsonProperty(
            "notes",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        )]
        public List<Note>? Notes
        {
            get => _notes;
            set => _notes = value ?? new List<Note>();
        } // note列表

        private List<Note>? _notes = new();

        /// <summary>
        /// 音符总数，严格按 RePhiEdit 规范的 numOfNotes 计算（包含 FakeNote、不包含 Hold），
        /// 不能反映 Note 的真实数量。除非你明确知道用途，否则请按自己的规则从 Notes 中计算。
        /// </summary>
        [JsonProperty("numOfNotes")]
        public int TotalNumberOfNotes => Notes?.Count(note => note.Type != NoteType.Hold) ?? 0;

        /// <summary>
        /// 特殊事件层（故事板）
        /// </summary>
        [JsonProperty(
            "extended",
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        )]
        public Events.ExtendLayer Extended { get; set; } = new();

        /// <summary>
        /// 判定线的Z轴顺序
        /// </summary>
        [JsonProperty("zOrder")]
        public int ZOrder { get; set; } // Z轴顺序

        /// <summary>
        /// 判定线是否绑定UI
        /// </summary>
        [JsonProperty("attachUI", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(AttachUiConverter))]
        public AttachUi? AttachUi { get; set; } // 绑定UI名，当不绑定时为null

        /// <summary>
        /// 判定线纹理是否为GIF
        /// </summary>
        [JsonProperty("isGif")]
        public bool IsGif { get; set; } // 纹理是否为GIF

        /// <summary>
        /// 所属组
        /// </summary>
        [JsonProperty("Group")]
        public int Group { get; set; } // 绑定组

        /// <summary>
        /// 当前判定线相对于当前BPM的因子。判定线BPM = 当前BPM / BpmFactor
        /// </summary>
        [JsonProperty("bpmfactor")]
        public float BpmFactor { get; set; } = 1.0f; // BPM因子

        /// <summary>
        /// 是否跟随父线旋转
        /// </summary>
        [JsonProperty("rotateWithFather")]
        public bool RotateWithFather { get; set; } // 是否随父级旋转

        /// <summary>
        /// Position（X） Control 控制点列表
        /// </summary>
        [JsonProperty("posControl")]
        public List<Controls.XControl> PositionControls
        {
            get
            {
                _positionControls ??= Controls.XControl.Default;

                return _positionControls;
            }
            set => _positionControls = value;
        }

        [JsonIgnore]
        private List<Controls.XControl>? _positionControls;

        /// <summary>
        /// Alpha Control 控制点列表
        /// </summary>
        [JsonProperty("alphaControl")]
        public List<Controls.AlphaControl> AlphaControls
        {
            get
            {
                _alphaControls ??= Controls.AlphaControl.Default;

                return _alphaControls;
            }
            set => _alphaControls = value;
        }

        [JsonIgnore]
        private List<Controls.AlphaControl>? _alphaControls;

        /// <summary>
        /// Size Control 控制点列表
        /// </summary>
        [JsonProperty("sizeControl")]
        public List<Controls.SizeControl> SizeControls
        {
            get
            {
                _sizeControls ??= Controls.SizeControl.Default;

                return _sizeControls;
            }
            set => _sizeControls = value;
        }

        [JsonIgnore]
        private List<Controls.SizeControl>? _sizeControls;

        /// <summary>
        /// Skew Control 控制点列表
        /// </summary>
        [JsonProperty("skewControl")]
        public List<Controls.SkewControl> SkewControls
        {
            get
            {
                _skewControls ??= Controls.SkewControl.Default;

                return _skewControls;
            }
            set => _skewControls = value;
        }

        [JsonIgnore]
        private List<Controls.SkewControl>? _skewControls;

        /// <summary>
        /// Y Control 控制点列表
        /// </summary>
        [JsonProperty("yControl")]
        public List<Controls.YControl> YControls
        {
            get
            {
                _yControls ??= Controls.YControl.Default;

                return _yControls;
            }
            set => _yControls = value;
        }

        [JsonIgnore]
        private List<Controls.YControl>? _yControls;

        /// <summary>
        /// 深拷贝当前判定线及其可变子对象。
        /// </summary>
        /// <returns>与当前判定线数据一致且相互独立的副本</returns>
        public JudgeLine Clone()
        {
            return new JudgeLine
            {
                Name = Name,
                Texture = Texture,
                Anchor = Anchor.ToArray(),
                EventLayers = EventLayers.ConvertAll(eventLayer => eventLayer.Clone()),
                Father = Father,
                IsCover = IsCover,
                Notes = Notes?.ConvertAll(note => note.Clone()) ?? new List<Note>(),
                Extended = Extended.Clone(),
                ZOrder = ZOrder,
                AttachUi = AttachUi,
                IsGif = IsGif,
                Group = Group,
                BpmFactor = BpmFactor,
                RotateWithFather = RotateWithFather,
                PositionControls = PositionControls.ConvertAll(control =>
                    (Controls.XControl)control.Clone()
                ),
                AlphaControls = AlphaControls.ConvertAll(control =>
                    (Controls.AlphaControl)control.Clone()
                ),
                SizeControls = SizeControls.ConvertAll(control =>
                    (Controls.SizeControl)control.Clone()
                ),
                SkewControls = SkewControls.ConvertAll(control =>
                    (Controls.SkewControl)control.Clone()
                ),
                YControls = YControls.ConvertAll(control =>
                    (Controls.YControl)control.Clone()
                ),
            };
        }
    }
}
