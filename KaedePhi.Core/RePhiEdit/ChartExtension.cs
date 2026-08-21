using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KaedePhi.Core.Common;
using Newtonsoft.Json;

namespace KaedePhi.Core.RePhiEdit
{
    public partial class Chart
    {
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
        /// 序列化谱面
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>Json</returns>
        [PublicAPI]
        public string ExportToJson(bool format)
        {
            Anticipation();
            return JsonConvert.SerializeObject(
                this,
                format ? Formatting.Indented : Formatting.None
            );
        }

        /// <summary>
        /// 将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public void ExportToJsonStream(Stream stream, bool format)
        {
            Anticipation();
            using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = JsonDefaults.CreateSerializer(
                format ? Formatting.Indented : Formatting.None
            );

            using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
            serializer.Serialize(jsonWriter, this);
            jsonWriter.Flush();
            streamWriter.Flush();
        }

        /// <summary>
        /// 异步将谱面序列化为Json并写入流
        /// </summary>
        /// <param name="stream">流</param>
        /// <param name="format">是否需要格式化</param>
        public async Task ExportToJsonStreamAsync(Stream stream, bool format)
        {
            Anticipation();
            await using var streamWriter = new StreamWriter(
                stream,
                JsonDefaults.NoBomUtf8,
                1024,
                leaveOpen: true
            );
            var serializer = JsonDefaults.CreateSerializer(
                format ? Formatting.Indented : Formatting.None
            );

            await Task.Run(() =>
            {
                using var jsonWriter = new JsonTextWriter(streamWriter) { CloseOutput = false };
                serializer.Serialize(jsonWriter, this);
                jsonWriter.Flush();
            });

            await streamWriter.FlushAsync();
        }

        /// <summary>
        /// 异步序列化为Json
        /// </summary>
        /// <param name="format">是否需要格式化</param>
        /// <returns>json</returns>
        public Task<string> ExportToJsonAsync(bool format) => Task.Run(() => ExportToJson(format));

        /// <summary>
        /// 从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面对象</returns>
        /// <exception cref="InvalidOperationException">谱面json数据无法正确序列化</exception>
        [PublicAPI]
        public static Chart LoadFromJson(string json)
        {
            var chart =
                JsonConvert.DeserializeObject<Chart>(json, JsonDefaults.DeserializeSettings)
                ?? throw new InvalidOperationException("Failed to deserialize Chart from JSON.");
            foreach (
                var eventLayer in chart.JudgeLineList.SelectMany(judgeLine =>
                {
                    judgeLine.EventLayers.RemoveAll(layer => (object?)layer is null);
                    return judgeLine.EventLayers;
                })
            )
                eventLayer.Sort();

            return chart;
        }

        /// <summary>
        /// 异步从Json反序列化
        /// </summary>
        /// <param name="json">谱面Json数据</param>
        /// <returns>谱面</returns>
        public static Task<Chart> LoadFromJsonAsync(string json) =>
            Task.Run(() => LoadFromJson(json));

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Chart LoadFromStream(Stream stream)
        {
            using var streamReader = new StreamReader(
                stream,
                JsonDefaults.NoBomUtf8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = JsonDefaults.CreateSerializer(Formatting.None);
            var chart =
                serializer.Deserialize<Chart>(jsonReader)
                ?? throw new InvalidOperationException("Failed to deserialize Chart from stream.");

            foreach (
                var eventLayer in chart.JudgeLineList.SelectMany(judgeLine =>
                {
                    judgeLine.EventLayers.RemoveAll(layer => (object?)layer is null);
                    return judgeLine.EventLayers;
                })
            )
                eventLayer.Sort();

            return chart;
        }

        /// <summary>
        /// 从流反序列化
        /// </summary>
        /// <param name="stream">流</param>
        /// <returns>谱面</returns>
        /// <exception cref="InvalidOperationException">反序列化失败</exception>
        public static Task<Chart> LoadFromStreamAsync(Stream stream)
        {
            try
            {
                using var streamReader = new StreamReader(
                    stream,
                    JsonDefaults.NoBomUtf8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true
                );
                using var jsonReader = new JsonTextReader(streamReader);
                var serializer = JsonDefaults.CreateSerializer(Formatting.None);
                var chart =
                    serializer.Deserialize<Chart>(jsonReader)
                    ?? throw new InvalidOperationException(
                        "Failed to deserialize Chart from stream."
                    );

                foreach (
                    var eventLayer in chart.JudgeLineList.SelectMany(judgeLine =>
                    {
                        judgeLine.EventLayers.RemoveAll(layer => (object?)layer is null);
                        return judgeLine.EventLayers;
                    })
                )
                    eventLayer.Sort();

                return Task.FromResult(chart);
            }
            catch (Exception exception)
            {
                return Task.FromException<Chart>(exception);
            }
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
