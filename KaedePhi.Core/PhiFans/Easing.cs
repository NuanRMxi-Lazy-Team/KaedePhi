using KaedePhi.Core.PhiFans.JsonConverter;
using Newtonsoft.Json;

namespace KaedePhi.Core.PhiFans
{
    /// <summary>
    /// 缓动类型封装，编号范围 0-30。
    /// 结构与 PhiEdit 的缓动类型一致：缓存缓动函数，
    /// 插值仅需起止值与归一化时间，不包含 KPC/RePhiEdit 的截取参数（PhiFans 无此概念）。
    /// </summary>
    [JsonConverter(typeof(EasingJsonConverter))]
    public class Easing
    {
        private static readonly Easing?[] Cache = new Easing[31];

        /// <summary>
        /// 线性缓动（编号0）。
        /// </summary>
        public static Easing Linear { get; } = Get(0);

        private readonly int _easingNumber;
        private readonly Utils.Easings.EasingFunction _function;

        /// <summary>获取缓存的 Easing 实例，避免重复创建。</summary>
        /// <param name="easingNumber">缓动编号</param>
        /// <returns>缓动类型实例</returns>
        public static Easing Get(int easingNumber)
        {
            if (easingNumber is >= 0 and <= 30)
                return Cache[easingNumber] ??= new Easing(easingNumber);
            return new Easing(easingNumber);
        }

        /// <summary>
        /// 创建指定编号的缓动类型。
        /// </summary>
        /// <param name="easingNumber">缓动编号</param>
        public Easing(int easingNumber)
        {
            _easingNumber = easingNumber;
            _function = Easings.GetFunction(easingNumber);
        }

        /// <summary>对 [start, end] 区间在归一化时间 t 处进行插值</summary>
        /// <param name="start">开始数值</param>
        /// <param name="end">结束数值</param>
        /// <param name="t">归一化时间（0 到 1）</param>
        /// <returns>插值结果</returns>
        public float Interpolate(float start, float end, float t)
        {
            var easedTime = _function(t);
            return (float)(start + (end - start) * easedTime);
        }

        /// <inheritdoc cref="Interpolate(float,float,float)"/>
        /// <param name="start">开始数值</param>
        /// <param name="end">结束数值</param>
        /// <param name="t">归一化时间（0 到 1）</param>
        /// <returns>插值结果</returns>
        public double Interpolate(double start, double end, double t)
        {
            var easedTime = _function(t);
            return start + (end - start) * easedTime;
        }

        /// <summary>
        /// 隐式转换为 int，返回缓动编号。
        /// </summary>
        public static implicit operator int(Easing easing) => easing._easingNumber;

        /// <summary>
        /// 从缓动编号隐式创建缓动类型。
        /// </summary>
        public static implicit operator Easing(int easingNumber) => Get(easingNumber);

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
