using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model.Controls
{
    public abstract class ControlBase
    {
        [JsonProperty("easing")]
        public Easing Easing { get; set; } = new(1);

        [JsonProperty("x")]
        public float X { get; set; }

        public abstract ControlBase Clone();
    }
}
