using System;

namespace PhiFanmade.Core.Utils
{
    public static class Bezier
    {
        public static T Do<T>(float[] points, float t, T startValue, T endValue, float left = 0.0f,
            float right = 1.0f)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            // 验证类型
            var type = typeof(T);
            if (type != typeof(float) && type != typeof(double) && type != typeof(int) && type != typeof(byte))
                throw new NotSupportedException("T must be float, double, int, or byte");


            // 将 t 映射到 [left, right] 区间
            var mappedT = left + t * (right - left);

            // 使用 De Casteljau 算法计算贝塞尔曲线值
            var n = points.Length;
            var temp = new float[n];
            Array.Copy(points, temp, n);

            for (var i = 1; i < n; i++)
            {
                for (var j = 0; j < n - i; j++)
                {
                    temp[j] = (1 - mappedT) * temp[j] + mappedT * temp[j + 1];
                }
            }

            // 在 startValue 和 endValue 之间插值
            var start = Convert.ToDouble(startValue);
            var end = Convert.ToDouble(endValue);
            var result = start + temp[0] * (end - start);

            return (T)Convert.ChangeType(result, typeof(T));
        }
    }
}