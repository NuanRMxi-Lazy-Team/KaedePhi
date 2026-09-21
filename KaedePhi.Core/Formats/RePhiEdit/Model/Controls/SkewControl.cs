using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model.Controls
{
    public class SkewControl : ControlBase
    {
        [JsonProperty("skew")]
        public float Skew { get; set; } = 1.0f;

        private static readonly List<SkewControl> DefaultInstance = new()
        {
            new SkewControl
            {
                Easing = Easing.Linear,
                Skew = 0.0f,
                X = 0.0f,
            },
            new SkewControl
            {
                Easing = Easing.Linear,
                Skew = 0.0f,
                X = 9999999.0f,
            },
        };

        [JsonIgnore]
        public static List<SkewControl> Default =>
            DefaultInstance.ConvertAll(input => (SkewControl)input.Clone());

        public override ControlBase Clone()
        {
            return new SkewControl
            {
                Easing = new Easing(Easing),
                X = X,
                Skew = Skew,
            };
        }
    }
}
