using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Model.Controls
{
    /// <summary>
    /// 倾斜控制点。
    /// </summary>
    public class SkewControl : ControlBase
    {
        /// <summary>
        /// 倾斜值。
        /// </summary>
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

        /// <summary>
        /// 获取默认控制点列表。
        /// </summary>
        public static List<SkewControl> Default =>
            DefaultInstance.ConvertAll(input => (SkewControl)input.Clone());

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
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
