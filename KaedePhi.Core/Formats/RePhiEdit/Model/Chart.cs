using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model
{
    public partial class Chart
    {
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static Common.CoordinateSystem CoordinateSystemInstance { get; } = new()
        {
            MaxX = 675f,
            MinX = -675f,
            MaxY = 450f,
            MinY = -450f,
            ClockwiseRotation = true,
        };

        /// <summary>
        /// BPM列表
        /// </summary>
        [JsonProperty("BPMList")]
        public List<BpmItem> BpmList { get; set; } = new();

        /// <summary>
        /// 元数据
        /// </summary>
        [JsonProperty("META")]
        public Meta Meta { get; set; } = new();

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")]
        public List<JudgeLine> JudgeLineList { get; set; } = new();

        /// <summary>
        /// 制谱时长（秒）
        /// </summary>
        [JsonProperty("chartTime")]
        public double ChartTime { get; set; }

        /// <summary>
        /// 判定线组
        /// </summary>
        [JsonProperty("judgeLineGroup")]
        public string[] JudgeLineGroup { get; set; } = { "Default" };

        /// <summary>
        /// 多线编辑判定线列表（以空格为分割，或使用x:y选中x~y所有判定线）
        /// </summary>
        [JsonProperty("multiLineString")]
        public string MultiLineString { get; set; } = "1";

        /// <summary>
        /// 多线编辑页面缩放比例
        /// </summary>
        [JsonProperty("multiScale")]
        public float MultiScale { get; set; } = 1.0f;

        /// <summary>
        /// RPE右上角进度条显示的标记
        /// </summary>
        [JsonProperty("timeTags")]
        public List<BeatTag> BeatTags { get; set; } = new();

        /// <summary>
        /// XY事件是否一一对应
        /// </summary>
        // ReSharper disable once StringLiteralTypo
        [JsonProperty("xybind")]
        public bool XyBind { get; set; } = true;

        /// <summary>
        /// 对判定线及其事件层级进行预处理。
        /// </summary>
        [PublicAPI]
        public void Anticipation()
        {
            foreach (var judgeLine in JudgeLineList)
            {
                // 如果这个判定线层级上有null层级，移除它们
                judgeLine.EventLayers.RemoveAll(layer => (object?)layer is null);
                // 对所有判定线的所有事件层级执行Anticipation()方法
                foreach (var eventLayer in judgeLine.EventLayers)
                {
                    eventLayer.Anticipation();
                    eventLayer.Sort();
                }

                judgeLine.Extended.Anticipation();

                // 如果判定线上有任何类型的Control组为空或null，则设定一个默认值
                if (
                    ControlsIsNullOrEmpty(
                        judgeLine.AlphaControls.Cast<Controls.ControlBase>().ToList()
                    )
                )
                    judgeLine.AlphaControls = Controls.AlphaControl.Default;
                if (
                    ControlsIsNullOrEmpty(
                        judgeLine.PositionControls.Cast<Controls.ControlBase>().ToList()
                    )
                )
                    judgeLine.PositionControls = Controls.XControl.Default;
                if (
                    ControlsIsNullOrEmpty(
                        judgeLine.SizeControls.Cast<Controls.ControlBase>().ToList()
                    )
                )
                    judgeLine.SizeControls = Controls.SizeControl.Default;
                if (
                    ControlsIsNullOrEmpty(
                        judgeLine.SkewControls.Cast<Controls.ControlBase>().ToList()
                    )
                )
                    judgeLine.SkewControls = Controls.SkewControl.Default;
                if (
                    ControlsIsNullOrEmpty(judgeLine.YControls.Cast<Controls.ControlBase>().ToList())
                )
                    judgeLine.YControls = Controls.YControl.Default;

                // 如果判定线没有任何音符，则将音符列表设置为null
                if (judgeLine.Notes?.Count == 0)
                    judgeLine.Notes = null;
            }
        }

        private static bool ControlsIsNullOrEmpty(List<Controls.ControlBase>? controls)
        {
            return controls is null || controls.Count == 0;
        }

        /// <summary>
        /// 深拷贝当前谱面及其可变子对象。
        /// </summary>
        /// <returns>与当前谱面数据一致且相互独立的副本</returns>
        public Chart Clone()
        {
            return new Chart
            {
                BpmList = BpmList.ConvertAll(bpm => bpm.Clone()),
                Meta = Meta.Clone(),
                JudgeLineList = JudgeLineList.ConvertAll(judgeLine => judgeLine.Clone()),
                ChartTime = ChartTime,
                JudgeLineGroup = JudgeLineGroup.ToArray(),
                MultiLineString = MultiLineString,
                MultiScale = MultiScale,
                BeatTags = BeatTags.ConvertAll(tag => new BeatTag
                {
                    Name = tag.Name,
                    Time = tag.Time,
                }),
                XyBind = XyBind,
            };
        }
    }
}