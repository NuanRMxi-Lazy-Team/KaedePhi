using System.Collections.Generic;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.RePhiEdit.Model.Controls
{
    public class SizeControl : ControlBase
    {
        [JsonProperty("size")]
        public float Size { get; set; } = 1.0f;

        private static readonly List<SizeControl> DefaultInstance = new()
        {
            new SizeControl
            {
                Easing = Easing.Linear,
                Size = 1.0f,
                X = 0.0f,
            },
            new SizeControl
            {
                Easing = Easing.Linear,
                Size = 1.0f,
                X = 9999999.0f,
            },
        };

        [JsonIgnore]
        public static List<SizeControl> Default =>
            DefaultInstance.ConvertAll(input => (SizeControl)input.Clone());

        public override ControlBase Clone()
        {
            return new SizeControl
            {
                Easing = new Easing(Easing),
                X = X,
                Size = Size,
            };
        }
    }
}
