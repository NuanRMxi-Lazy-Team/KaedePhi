namespace KaedePhi.Core.Intermediate
{
    /// <summary>
    /// 缓动类型封装。
    /// </summary>
    public class Easing
    {
        /// <summary>
        /// 线性缓动（编号1）。
        /// </summary>
        public static Easing Linear { get; } = new(1);

        /// <summary>
        /// 创建指定编号的缓动类型。
        /// </summary>
        /// <param name="easingNumber">缓动编号</param>
        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
        }

        private readonly int _easingNumber;

        /// <summary>
        /// 在指定缓动函数的 minLim 和 maxLim 之间对 [start, end] 区间在 t 处进行插值
        /// </summary>
        /// <param name="minLim">缓动函数左界限</param>
        /// <param name="maxLim">缓动函数右界限</param>
        /// <param name="start">开始数值</param>
        /// <param name="end">结束数值</param>
        /// <param name="t">插值点</param>
        /// <returns>插值结果</returns>
        public float Interpolate(float minLim, float maxLim, float start, float end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            //插值后返回
            return (float)(start + (end - start) * easedTime);
        }

        /// <inheritdoc cref="Interpolate(float,float,float,float,float)"/>
        public double Interpolate(float minLim, float maxLim, double start, double end, double t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            //插值后返回
            return start + (end - start) * easedTime;
        }

        /// <inheritdoc cref="Interpolate(float,float,float,float,float)"/>
        public int Interpolate(float minLim, float maxLim, int start, int end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            //插值后返回
            return (int)(start + (end - start) * easedTime);
        }

        /// <inheritdoc cref="Interpolate(float,float,float,float,float)"/>
        public byte Interpolate(float minLim, float maxLim, byte start, byte end, float t)
        {
            var easedTime = Easings.Evaluate(_easingNumber, minLim, maxLim, t);
            //插值后返回
            return (byte)(start + (end - start) * easedTime);
        }

        /// <summary>
        /// 隐式转换为 int，返回缓动编号。
        /// </summary>
        public static implicit operator int(Easing easing) => easing._easingNumber;

        /// <summary>
        /// 返回缓动函数名称。
        /// </summary>
        /// <returns>缓动函数名称</returns>
        public override string ToString()
        {
            // 返回缓动函数名称
            return _easingNumber switch
            {
                1 => "Linear",
                // 正弦
                2 => "EaseInSine",
                3 => "EaseOutSine",
                4 => "EaseInOutSine",
                // 二次
                5 => "EaseInQuad",
                6 => "EaseOutQuad",
                7 => "EaseInOutQuad",
                // 三次
                8 => "EaseInCubic",
                9 => "EaseOutCubic",
                10 => "EaseInOutCubic",
                // 四次
                11 => "EaseInQuart",
                12 => "EaseOutQuart",
                13 => "EaseInOutQuart",
                // 五次
                14 => "EaseInQuint",
                15 => "EaseOutQuint",
                16 => "EaseInOutQuint",
                // 指数
                17 => "EaseInExpo",
                18 => "EaseOutExpo",
                19 => "EaseInOutExpo",
                // 圆形
                20 => "EaseInCirc",
                21 => "EaseOutCirc",
                22 => "EaseInOutCirc",
                // 回弹
                23 => "EaseInBack",
                24 => "EaseOutBack",
                25 => "EaseInOutBack",
                // 弹性
                26 => "EaseInElastic",
                27 => "EaseOutElastic",
                28 => "EaseInOutElastic",
                // 弹跳
                29 => "EaseInBounce",
                30 => "EaseOutBounce",
                31 => "EaseInOutBounce",
                // 兜底
                _ => $"Unknown({_easingNumber})",
            };
        }
    }
}
