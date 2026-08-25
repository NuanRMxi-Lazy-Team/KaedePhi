using System;
using System.Collections.Generic;
using KaedePhi.Core.Common;

namespace KaedePhi.Core.KaedePhi
{
    public class JudgeLine
    {
        private float _bpmFactor = 1f;

        /// <summary>
        /// 判定线名称
        /// </summary>
        public string Name { get; set; } = "KpcJudgeLine";

        /// <summary>
        /// 判定线纹理相对路径，默认值为line.png
        /// </summary>
        public string Texture { get; set; } = CoreConstants.DefaultTexture;

        /// <summary>
        /// 判定线纹理锚点(0~1之间)，默认值为中心点(0.5, 0.5)
        /// </summary>
        public float[] Anchor { get; set; } = { 0.5f, 0.5f }; // 判定线纹理锚点

        /// <summary>
        /// 判定线事件层列表
        /// </summary>
        public List<Events.EventLayer> EventLayers { get; set; } = new(); // 事件层

        /// <summary>
        /// 父级判定线索引，-1表示无父级
        /// </summary>
        public int Father { get; set; } = -1; // 父级

        /// <summary>
        /// 是否遮罩越过判定线的音符（已被打击的除外）
        /// </summary>
        public bool IsCover { get; set; } = true; // 是否遮罩

        /// <summary>
        /// 判定线音符列表
        /// </summary>
        public List<Note> Notes { get; set; } = new();

        /// <summary>
        /// 特殊事件层（故事板）
        /// </summary>
        public Events.ExtendLayer Extended { get; set; } = new();

        /// <summary>
        /// 判定线的Z轴顺序
        /// </summary>
        public int ZOrder { get; set; } // Z轴顺序

        /// <summary>
        /// 判定线是否绑定UI
        /// </summary>
        public AttachUi? AttachUi { get; set; } // 绑定UI名，当不绑定时为null

        /// <summary>
        /// 判定线纹理是否为GIF
        /// </summary>
        public bool IsGif { get; set; } // 纹理是否为GIF

        /// <summary>
        /// 当前判定线相对于当前BPM的因子。判定线BPM = 谱面BPM / BpmFactor
        /// </summary>
        public float BpmFactor
        {
            get => _bpmFactor;
            set
            {
                if (!float.IsFinite(value) || value <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(BpmFactor),
                        "BPM 因子必须是有限正数。"
                    );
                _bpmFactor = value;
            }
        }

        /// <summary>
        /// 是否跟随父线旋转
        /// </summary>
        public bool RotateWithFather { get; set; }

        /// <summary>
        /// Position（X） Control 控制点列表
        /// </summary>
        public List<Controls.XControl> PositionControls
        {
            get
            {
                _positionControls ??= Controls.XControl.Default;

                return _positionControls;
            }
            set => _positionControls = value;
        }

        private List<Controls.XControl>? _positionControls;

        /// <summary>
        /// Alpha Control 控制点列表
        /// </summary>
        public List<Controls.AlphaControl> AlphaControls
        {
            get
            {
                _alphaControls ??= Controls.AlphaControl.Default;

                return _alphaControls;
            }
            set => _alphaControls = value;
        }

        private List<Controls.AlphaControl>? _alphaControls;

        /// <summary>
        /// Size Control 控制点列表
        /// </summary>
        public List<Controls.SizeControl> SizeControls
        {
            get
            {
                _sizeControls ??= Controls.SizeControl.Default;

                return _sizeControls;
            }
            set => _sizeControls = value;
        }

        private List<Controls.SizeControl>? _sizeControls;

        /// <summary>
        /// Skew Control 控制点列表
        /// </summary>
        public List<Controls.SkewControl> SkewControls
        {
            get
            {
                _skewControls ??= Controls.SkewControl.Default;

                return _skewControls;
            }
            set => _skewControls = value;
        }

        private List<Controls.SkewControl>? _skewControls;

        /// <summary>
        /// Y Control 控制点列表
        /// </summary>
        public List<Controls.YControl> YControls
        {
            get
            {
                _yControls ??= Controls.YControl.Default;

                return _yControls;
            }
            set => _yControls = value;
        }

        private List<Controls.YControl>? _yControls;

        /// <summary>
        /// 深拷贝判定线。
        /// </summary>
        /// <returns>判定线副本</returns>
        public JudgeLine Clone()
        {
            var clone = new JudgeLine
            {
                Name = Name,
                Texture = Texture,
                Anchor = (float[])Anchor.Clone(),
                Father = Father,
                IsCover = IsCover,
                ZOrder = ZOrder,
                IsGif = IsGif,
                BpmFactor = BpmFactor,
                RotateWithFather = RotateWithFather,
                AttachUi = AttachUi,
                EventLayers = new List<Events.EventLayer>(),
                Notes = new List<Note>(),
                Extended = Extended.Clone(),
                PositionControls = new List<Controls.XControl>(),
                AlphaControls = new List<Controls.AlphaControl>(),
                SizeControls = new List<Controls.SizeControl>(),
                SkewControls = new List<Controls.SkewControl>(),
                YControls = new List<Controls.YControl>(),
            };

            // 深拷贝列表；空集合视为正常谱面，跳过空元素
            if (EventLayers is not null)
            {
                foreach (var eventLayer in EventLayers)
                {
                    if (eventLayer is null)
                        continue;
                    clone.EventLayers.Add(eventLayer.Clone());
                }
            }

            if (Notes is not null)
            {
                foreach (var note in Notes)
                {
                    if (note is null)
                        continue;
                    clone.Notes.Add(note.Clone());
                }
            }

            foreach (var control in PositionControls)
                clone.PositionControls.Add((Controls.XControl)control.Clone());
            foreach (var control in AlphaControls)
                clone.AlphaControls.Add((Controls.AlphaControl)control.Clone());
            foreach (var control in SizeControls)
                clone.SizeControls.Add((Controls.SizeControl)control.Clone());
            foreach (var control in SkewControls)
                clone.SkewControls.Add((Controls.SkewControl)control.Clone());
            foreach (var control in YControls)
                clone.YControls.Add((Controls.YControl)control.Clone());

            return clone;
        }
    }
}
