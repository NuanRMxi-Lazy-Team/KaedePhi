using KaedePhi.Core.PhiFans.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    /// <summary>
    /// 缓动类型封装，编号范围 0-30。
    /// </summary>
    [JsonConverter(typeof(EasingJsonConverter))]
    public class Easing
    {
        /// <summary>
        /// 线性缓动（编号0）。
        /// </summary>
        public static Easing Linear { get; } = new(0);

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
                0 => "Linear",
                // 正弦
                1 => "EaseInSine",
                2 => "EaseOutSine",
                3 => "EaseInOutSine",
                // 二次
                4 => "EaseInQuad",
                5 => "EaseOutQuad",
                6 => "EaseInOutQuad",
                // 三次
                7 => "EaseInCubic",
                8 => "EaseOutCubic",
                9 => "EaseInOutCubic",
                // 四次
                10 => "EaseInQuart",
                11 => "EaseOutQuart",
                12 => "EaseInOutQuart",
                // 五次
                13 => "EaseInQuint",
                14 => "EaseOutQuint",
                15 => "EaseInOutQuint",
                // 指数
                16 => "EaseInExpo",
                17 => "EaseOutExpo",
                18 => "EaseInOutExpo",
                // 圆形
                19 => "EaseInCirc",
                20 => "EaseOutCirc",
                21 => "EaseInOutCirc",
                // 回弹
                22 => "EaseInBack",
                23 => "EaseOutBack",
                24 => "EaseInOutBack",
                // 弹性
                25 => "EaseInElastic",
                26 => "EaseOutElastic",
                27 => "EaseInOutElastic",
                // 弹跳
                28 => "EaseInBounce",
                29 => "EaseOutBounce",
                30 => "EaseInOutBounce",
                // 兜底
                _ => $"Unknown({_easingNumber})",
            };
        }
    }
}
