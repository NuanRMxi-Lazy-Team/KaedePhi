using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Controls
{
    /// <summary>
    /// 不透明度控制点。
    /// </summary>
    public class AlphaControl : ControlBase
    {
        /// <summary>
        /// 不透明度值。
        /// </summary>
        public float Alpha { get; set; } = 1.0f;

        private static readonly List<AlphaControl> DefaultInstance = new()
        {
            new AlphaControl
            {
                Easing = Easing.Linear,
                Alpha = 1.0f,
                X = 0.0f,
            },
            new AlphaControl
            {
                Easing = Easing.Linear,
                Alpha = 1.0f,
                X = 9999999.0f,
            },
        };

        /// <summary>
        /// 获取默认控制点列表。
        /// </summary>
        public static List<AlphaControl> Default =>
            DefaultInstance.ConvertAll(input => (AlphaControl)input.Clone());

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
        public override ControlBase Clone()
        {
            return new AlphaControl
            {
                Easing = new Easing(Easing),
                X = X,
                Alpha = Alpha,
            };
        }
    }
}
