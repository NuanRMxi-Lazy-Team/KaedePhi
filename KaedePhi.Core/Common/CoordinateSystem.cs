namespace KaedePhi.Core.Common
{
    /// <summary>
    /// 判定线坐标系信息，描述判定线在谱面中的坐标范围与旋转方向。
    /// </summary>
    public struct CoordinateSystem
    {
        /// <summary>X 轴最大值</summary>
        public float MaxX;

        /// <summary>X 轴最小值</summary>
        public float MinX;

        /// <summary>Y 轴最大值</summary>
        public float MaxY;

        /// <summary>Y 轴最小值</summary>
        public float MinY;

        /// <summary>旋转方向是否为顺时针</summary>
        public bool ClockwiseRotation;
    }
}