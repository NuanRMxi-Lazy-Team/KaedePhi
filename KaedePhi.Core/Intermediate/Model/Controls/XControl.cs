using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Model.Controls
{
    /// <summary>
    /// X 轴位置控制点。
    /// </summary>
    public class XControl : ControlBase
    {
        /// <summary>
        /// 位置值。
        /// </summary>
        public float Pos { get; set; } = 1.0f;

        private static readonly List<XControl> DefaultInstance = new()
        {
            new XControl
            {
                Easing = Easing.Linear,
                Pos = 1.0f,
                X = 0.0f,
            },
            new XControl
            {
                Easing = Easing.Linear,
                Pos = 1.0f,
                X = 9999999.0f,
            },
        };

        /// <summary>
        /// 获取默认控制点列表。
        /// </summary>
        public static List<XControl> Default =>
            DefaultInstance.ConvertAll(input => (XControl)input.Clone());

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
        public override ControlBase Clone()
        {
            return new XControl
            {
                Easing = new Easing(Easing),
                X = X,
                Pos = Pos,
            };
        }
    }
}
