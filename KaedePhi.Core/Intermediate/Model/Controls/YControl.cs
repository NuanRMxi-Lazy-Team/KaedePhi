using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Controls
{
    /// <summary>
    /// Y 轴位置控制点。
    /// </summary>
    public class YControl : ControlBase
    {
        /// <summary>
        /// Y 轴位置值。
        /// </summary>
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

        /// <summary>
        /// 获取默认控制点列表。
        /// </summary>
        public static List<YControl> Default =>
            DefaultInstance.ConvertAll(input => (YControl)input.Clone());

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
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
