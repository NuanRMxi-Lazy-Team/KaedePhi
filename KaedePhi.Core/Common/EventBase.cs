using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KaedePhi.Core.Common
{
    /// <summary>
    /// 事件基类，包含 KPC 和 RPE 共享的属性、类型转换和克隆逻辑。
    /// 缓动插值由各子类自行实现（因 Easing 类型不同）。
    /// </summary>
    /// <typeparam name="T">事件值类型（int/float/double/byte/byte[]）</typeparam>
    public abstract class EventBase<T>
        where T : notnull
    {
        /// <summary>
        /// 是否为贝塞尔曲线
        /// </summary>
        public bool IsBezier { get; set; }

        /// <summary>
        /// 贝塞尔曲线控制点（x1 y1 x2 y2）
        /// </summary>
        public float[] BezierPoints { get; set; } = new float[4];

        /// <summary>
        /// 缓动截取左界限
        /// </summary>
        public float EasingLeft { get; set; }

        /// <summary>
        /// 缓动截取右界限
        /// </summary>
        public float EasingRight { get; set; } = 1.0f;

        /// <summary>
        /// 事件开始数值
        /// </summary>
        [DisallowNull]
        [NotNull]
        public T StartValue { get; set; } = CreateDefaultValue();

        /// <summary>
        /// 事件结束数值
        /// </summary>
        [DisallowNull]
        [NotNull]
        public T EndValue { get; set; } = CreateDefaultValue();

        /// <summary>
        /// 事件开始拍
        /// </summary>
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 });

        /// <summary>
        /// 事件结束拍
        /// </summary>
        public Beat EndBeat { get; set; } = new(new[] { 1, 0, 1 });

        /// <summary>
        /// 当此事件为文字事件时，此值为字体文件相对路径
        /// </summary>
        public string? Font { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static TTo Cast<TFrom, TTo>(TFrom value) => Unsafe.As<TFrom, TTo>(ref value);

        /// <summary>
        /// 为引用类型 T（byte[]）提供非空默认值，避免非空契约暴露运行时 null。
        /// </summary>
        private static T CreateDefaultValue() =>
            typeof(T) == typeof(byte[]) ? (T)(object)Array.Empty<byte>() : default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetValueAsSingle(T value)
        {
            if (typeof(T) == typeof(float))
                return Cast<T, float>(value);
            if (typeof(T) == typeof(double))
                return (float)Cast<T, double>(value);
            if (typeof(T) == typeof(int))
                return Cast<T, int>(value);
            if (typeof(T) == typeof(byte))
                return Cast<T, byte>(value);
            return Convert.ToSingle(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetValueAsDouble(T value)
        {
            if (typeof(T) == typeof(double))
                return Cast<T, double>(value);
            if (typeof(T) == typeof(float))
                return Cast<T, float>(value);
            if (typeof(T) == typeof(int))
                return Cast<T, int>(value);
            return Convert.ToDouble(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetValueAsInt32(T value)
        {
            if (typeof(T) == typeof(int))
                return Cast<T, int>(value);
            if (typeof(T) == typeof(float))
                return (int)Cast<T, float>(value);
            if (typeof(T) == typeof(double))
                return (int)Cast<T, double>(value);
            if (typeof(T) == typeof(byte))
                return Cast<T, byte>(value);
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// 获取 StartValue 的 float 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetStartValueAsSingle() => GetValueAsSingle(StartValue);

        /// <summary>
        /// 获取 EndValue 的 float 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetEndValueAsSingle() => GetValueAsSingle(EndValue);

        /// <summary>
        /// 获取 StartValue 的 double 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetStartValueAsDouble() => GetValueAsDouble(StartValue);

        /// <summary>
        /// 获取 EndValue 的 double 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetEndValueAsDouble() => GetValueAsDouble(EndValue);

        /// <summary>
        /// 获取 StartValue 的 int 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetStartValueAsInt32() => GetValueAsInt32(StartValue);

        /// <summary>
        /// 获取 EndValue 的 int 表示。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEndValueAsInt32() => GetValueAsInt32(EndValue);

        /// <summary>
        /// 针对已知类型的深拷贝
        /// </summary>
        protected static TValue DeepClone<TValue>(TValue value)
        {
            if (value is null)
                throw new InvalidOperationException("克隆时值不能为 null。");

            var type = typeof(TValue);

            if (
                type == typeof(int)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(byte)
            )
                return value;

            if (type == typeof(string))
                return value;

            if (type == typeof(byte[]))
            {
                var arr = Cast<TValue, byte[]>(value);
                return Cast<byte[], TValue>(arr.ToArray());
            }

            return value;
        }

        /// <summary>
        /// 将基类属性复制到目标实例。
        /// </summary>
        protected void CopyBaseTo(EventBase<T> target)
        {
            target.IsBezier = IsBezier;
            target.EasingLeft = EasingLeft;
            target.EasingRight = EasingRight;
            target.Font = Font;

            var bp = new float[BezierPoints.Length];
            Array.Copy(BezierPoints, bp, BezierPoints.Length);
            target.BezierPoints = bp;

            if (
                typeof(T) == typeof(int)
                || typeof(T) == typeof(float)
                || typeof(T) == typeof(double)
                || typeof(T) == typeof(byte)
                || typeof(T) == typeof(string)
            )
            {
                target.StartValue = StartValue;
                target.EndValue = EndValue;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                target.StartValue = StartValue is not null
                    ? Cast<byte[], T>(Cast<T, byte[]>(StartValue).ToArray())
                    : throw new InvalidOperationException("byte[] 克隆时 StartValue 不能为 null。");
                target.EndValue = EndValue is not null
                    ? Cast<byte[], T>(Cast<T, byte[]>(EndValue).ToArray())
                    : throw new InvalidOperationException("byte[] 克隆时 EndValue 不能为 null。");
            }
            else
            {
                target.StartValue = DeepClone(StartValue);
                target.EndValue = DeepClone(EndValue);
            }

            target.StartBeat = new Beat((int[])StartBeat);
            target.EndBeat = new Beat((int[])EndBeat);
        }
    }
}
