using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model.Controls
{
    public class YControl : ControlBase
    {
        [JsonProperty("y")]
        public float Y { get; set; } = 1.0f;

        private static readonly List<YControl> DefaultInstance = new()
        {
            new YControl
            {
                Easing = Easing.Linear,
                Y = 1.0f,
                X = 0.0f,
            },
            new YControl
            {
                Easing = Easing.Linear,
                Y = 1.0f,
                X = 9999999.0f,
            },
        };

        [JsonIgnore]
        public static List<YControl> Default =>
            DefaultInstance.ConvertAll(input => (YControl)input.Clone());

        public override ControlBase Clone()
        {
            return new YControl
            {
                Easing = new Easing(Easing),
                X = X,
                Y = Y,
            };
        }
    }
}
