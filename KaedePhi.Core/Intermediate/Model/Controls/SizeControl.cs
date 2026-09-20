using System.Collections.Generic;

namespace KaedePhi.Core.Intermediate.Controls
{
    /// <summary>
    /// 尺寸控制点。
    /// </summary>
    public class SizeControl : ControlBase
    {
        /// <summary>
        /// 尺寸值。
        /// </summary>
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

        /// <summary>
        /// 获取默认控制点列表。
        /// </summary>
        public static List<SizeControl> Default =>
            DefaultInstance.ConvertAll(input => (SizeControl)input.Clone());

        /// <summary>
        /// 深拷贝控制点。
        /// </summary>
        /// <returns>控制点副本</returns>
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
