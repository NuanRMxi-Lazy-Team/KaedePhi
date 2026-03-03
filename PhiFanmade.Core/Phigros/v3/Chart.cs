using Newtonsoft.Json;
using System.Collections.Generic;
using PhiFanmade.Core.Common;

namespace PhiFanmade.Core.Phigros.v3
{
    public class Chart
    {
        /// <summary>
        /// 格式版本号
        /// </summary>
        [JsonProperty("formatVersion")] public int FormatVersion = 3;

        /// <summary>
        /// 谱面偏移，单位为秒
        /// </summary>
        [JsonProperty("offset")] public float Offset = 0;

        /// <summary>
        /// 判定线列表
        /// </summary>
        [JsonProperty("judgeLineList")] public List<JudgeLine> JudgeLineList = null;
        
        /// <summary>
        /// 坐标系边界
        /// </summary>
        public static class CoordinateSystem
        {
            public const CoordinateSystemAnchor Anchor = CoordinateSystemAnchor.ScreenLeftLowerCorner;
            public const bool ClockwiseRotation = false;
            public const float MaxX = 1f;
            public const float MinX = 0f;
            public const float MaxY = 1f;
            public const float MinY = 0f;
        }
    }
}