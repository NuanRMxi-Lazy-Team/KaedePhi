using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    public class BlockArea
    {
        [JsonProperty("topRightPercentage")] public PositionUnit TopRightPercentage { get; set; }

        [JsonProperty("bottomLeftPercentage")] public PositionUnit BottomLeftPercentage { get; set; }

        [JsonProperty("appearTime")] public float AppearTime { get; set; }

        [JsonProperty("enableTime")] public float EnableTime { get; set; }

        [JsonProperty("disableTime")] public float DisableTime { get; set; }

        [JsonProperty("disappearTime")] public float DisappearTime { get; set; }

        [JsonProperty("isSubtract")] public bool IsSubtract { get; set; }

        [JsonProperty("rotateEvents")] public List<AreaRotateEvent> RotateEvents { get; set; }
        [JsonProperty("moveEvents")] public List<AreaMoveEvent> MoveEvents { get; set; }
        [JsonProperty("scaleEvents")] public List<AreaScaleEvent> ScaleEvents { get; set; }
        
    }
}