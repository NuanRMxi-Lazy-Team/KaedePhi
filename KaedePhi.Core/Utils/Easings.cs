using System;

namespace KaedePhi.Core.Utils
{
    public static class Easings
    {
        // Delegate for easing functions
        public delegate double EasingFunction(double t);

        // Linear
        public static double Linear(double t) => t;

        // Quadratic
        public static double EaseInQuad(double t) => t * t;
        public static double EaseOutQuad(double t) => t * (2 - t);

        public static double EaseInOutQuad(double t) =>
            t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;

        // Cubic
        public static double EaseInCubic(double t) => t * t * t;

        public static double EaseOutCubic(double t)
        {
            t--;
            return t * t * t + 1;
        }

        public static double EaseInOutCubic(double t) =>
            t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;

        // Quartic
        public static double EaseInQuart(double t) => t * t * t * t;

        public static double EaseOutQuart(double t)
        {
            t--;
            return 1 - t * t * t * t;
        }

        public static double EaseInOutQuart(double t) =>
            t < 0.5 ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;

        // Quintic
        public static double EaseInQuint(double t) => t * t * t * t * t;

        public static double EaseOutQuint(double t)
        {
            t--;
            return t * t * t * t * t + 1;
        }

        public static double EaseInOutQuint(double t) =>
            t < 0.5 ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;

        // Sine
        public static double EaseInSine(double t) =>
            1 - Math.Cos(t * Math.PI / 2);

        public static double EaseOutSine(double t) =>
            Math.Sin(t * Math.PI / 2);

        public static double EaseInOutSine(double t) =>
            -0.5f * (Math.Cos(Math.PI * t) - 1);

        // Exponential
        public static double EaseInExpo(double t) =>
            t == 0 ? 0 : Math.Pow(2, 10 * (t - 1));

        public static double EaseOutExpo(double t) =>
            t == 1 ? 1 : 1 - Math.Pow(2, -10 * t);

        public static double EaseInOutExpo(double t)
        {
            if (t == 0 || t == 1) return t;
            return t < 0.5f
                ? 0.5f * Math.Pow(2, 20 * t - 10)
                : 1 - 0.5f * Math.Pow(2, -20 * t + 10);
        }

        // Circular
        public static double EaseInCirc(double t) =>
            1 - Math.Sqrt(1 - t * t);

        public static double EaseOutCirc(double t) =>
            Math.Sqrt(1 - (--t) * t);

        public static double EaseInOutCirc(double t) =>
            t < 0.5f
                ? 0.5f * (1 - Math.Sqrt(1 - 4 * t * t))
                : 0.5f * (Math.Sqrt(1 - 4 * (--t) * t) + 1);

        // Back
        public static double EaseInBack(double t)
        {
            const double s = 1.70158f;
            return t * t * ((s + 1) * t - s);
        }

        public static double EaseOutBack(double t)
        {
            const double s = 1.70158f;
            t--;
            return t * t * ((s + 1) * t + s) + 1;
        }

        public static double EaseInOutBack(double t)
        {
            const double s = 1.70158f * 1.525f;
            t *= 2;
            if (t < 1)
                return 0.5f * (t * t * ((s + 1) * t - s));
            t -= 2;
            return 0.5f * (t * t * ((s + 1) * t + s) + 2);
        }

        // Elastic
        public static double EaseInElastic(double t)
        {
            if (t == 0 || t == 1) return t;
            return -Math.Pow(2, 10 * (t - 1)) *
                   Math.Sin((t - 1.1f) * 5 * Math.PI);
        }

        public static double EaseOutElastic(double t)
        {
            if (t == 0 || t == 1) return t;
            return Math.Pow(2, -10 * t) *
                Math.Sin((t - 0.1f) * 5 * Math.PI) + 1;
        }

        public static double EaseInOutElastic(double t)
        {
            if (t == 0 || t == 1) return t;
            t *= 2;
            if (t < 1)
                return -0.5f * Math.Pow(2, 10 * (t - 1)) *
                       Math.Sin((t - 1.1f) * 5 * Math.PI);
            t--;
            return Math.Pow(2, -10 * t) *
                Math.Sin((t - 0.1f) * 5 * Math.PI) * 0.5f + 1;
        }

        // Bounce
        public static double EaseInBounce(double t) =>
            1 - EaseOutBounce(1 - t);

        public static double EaseOutBounce(double t)
        {
            const double n1 = 7.5625f;
            const double d1 = 2.75f;
            if (t < 1 / d1)
                return n1 * t * t;
            else if (t < 2 / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
        }

        public static double EaseInOutBounce(double t) =>
            t < 0.5f
                ? (1 - EaseOutBounce(1 - 2 * t)) * 0.5f
                : (EaseOutBounce(2 * t - 1) + 1) * 0.5f;
    }
}