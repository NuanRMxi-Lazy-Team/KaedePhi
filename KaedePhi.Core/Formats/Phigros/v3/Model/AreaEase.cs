using System;
using KaedePhi.Core.Formats.Phigros.v3.Serialization.JsonConverter;
using KaedePhi.Core.Utils;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Model
{
    /// <summary>
    /// 表示噪域事件的缓动类型，并提供对应的插值计算。
    /// </summary>
    [JsonConverter(typeof(AreaEaseJsonConverter))]
    public readonly struct AreaEase : IEquatable<AreaEase>
    {
        private const int EaseTypeCount = (int)AreaEaseType.One + 1;
        private const int SamplesPerEase = 100;
        private static readonly float[,] EaseTable = CreateEaseTable();

        /// <summary>
        /// 创建指定整数编号的噪域缓动类型。
        /// </summary>
        /// <param name="value">缓动编号；格式定义的有效范围为 0 到 14。</param>
        public AreaEase(int value)
        {
            Value = value;
        }

        /// <summary>
        /// 创建指定噪域缓动类型。
        /// </summary>
        /// <param name="type">缓动类型。</param>
        public AreaEase(AreaEaseType type)
            : this((int)type) { }

        /// <summary>
        /// 获取线性缓动类型。
        /// </summary>
        public static AreaEase Linear => new(AreaEaseType.Linear);

        /// <summary>
        /// 获取缓动类型对应的枚举值。
        /// </summary>
        public AreaEaseType Type => (AreaEaseType)Value;

        /// <summary>
        /// 获取缓动类型对应的原始整数编号。
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// 对两个数值按当前缓动类型进行插值。
        /// </summary>
        /// <param name="start">上一个关键帧的数值。</param>
        /// <param name="end">当前关键帧的目标数值。</param>
        /// <param name="progress">事件进度，通常位于 0 到 1 之间。</param>
        /// <returns>插值结果。</returns>
        public float Interpolate(float start, float end, float progress) =>
            Interpolate(start, end, progress, Value);

        /// <summary>
        /// 将缓动类型转换为原始整数编号。
        /// </summary>
        /// <param name="ease">待转换的缓动类型。</param>
        /// <returns>缓动编号。</returns>
        public static implicit operator int(AreaEase ease) => ease.Value;

        /// <summary>
        /// 从原始整数编号创建缓动类型。
        /// </summary>
        /// <param name="value">缓动编号。</param>
        /// <returns>对应的缓动类型。</returns>
        public static implicit operator AreaEase(int value) => new(value);

        /// <summary>
        /// 从缓动枚举创建缓动类型。
        /// </summary>
        /// <param name="type">缓动枚举值。</param>
        /// <returns>对应的缓动类型。</returns>
        public static implicit operator AreaEase(AreaEaseType type) => new(type);

        /// <summary>
        /// 将缓动类型转换为枚举值。
        /// </summary>
        /// <param name="ease">待转换的缓动类型。</param>
        /// <returns>对应的枚举值。</returns>
        public static implicit operator AreaEaseType(AreaEase ease) => ease.Type;

        /// <summary>
        /// 判断缓动类型是否相同。
        /// </summary>
        /// <param name="other">要比较的缓动类型。</param>
        /// <returns>编号相同时为 <see langword="true"/>。</returns>
        public bool Equals(AreaEase other) => Value == other.Value;

        /// <summary>
        /// 判断对象是否与当前缓动类型相同。
        /// </summary>
        /// <param name="obj">待比较的对象。</param>
        /// <returns>对象是编号相同缓动类型时为 <see langword="true"/>。</returns>
        public override bool Equals(object? obj) => obj is AreaEase other && Equals(other);

        /// <summary>
        /// 获取缓动类型编号的哈希值。
        /// </summary>
        /// <returns>当前缓动类型的哈希值。</returns>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// 比较两个缓动类型是否相同。
        /// </summary>
        /// <param name="left">左侧缓动类型。</param>
        /// <param name="right">右侧缓动类型。</param>
        /// <returns>编号相同时为 <see langword="true"/>。</returns>
        public static bool operator ==(AreaEase left, AreaEase right) => left.Equals(right);

        /// <summary>
        /// 比较两个缓动类型是否不同。
        /// </summary>
        /// <param name="left">左侧缓动类型。</param>
        /// <param name="right">右侧缓动类型。</param>
        /// <returns>编号不同时为 <see langword="true"/>。</returns>
        public static bool operator !=(AreaEase left, AreaEase right) => !left.Equals(right);

        /// <summary>
        /// 返回缓动名称；未定义的编号以 Unknown 格式显示。
        /// </summary>
        /// <returns>缓动名称或未知编号。</returns>
        public override string ToString() =>
            Enum.IsDefined(typeof(AreaEaseType), Value) ? Type.ToString() : $"Unknown({Value})";

        /// <summary>
        /// 按原版的百分比采样表和线性插值规则计算缓动值。
        /// </summary>
        /// <param name="progress">事件进度，通常位于 0 到 1 之间。</param>
        /// <param name="type">缓动类型的整数值，范围为 0 到 14。</param>
        /// <returns>计算得到的缓动值。</returns>
        /// <exception cref="ArgumentOutOfRangeException">进度不是有限数值或缓动类型超出范围。</exception>
        public static float GetEaseWithProgress(float progress, int type)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    "进度必须是有限数值。"
                );
            if (type is < 0 or >= EaseTypeCount)
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    "缓动类型必须处于 0 到 14 之间。"
                );

            if (progress >= 1f)
                return EaseTable[type, SamplesPerEase];

            var scaledProgress = progress * SamplesPerEase;
            if (scaledProgress <= -1f)
                return EaseTable[type, 0];

            var index = (int)scaledProgress;
            switch (index)
            {
                case < 0:
                    return EaseTable[type, 0];
                case >= SamplesPerEase:
                    return EaseTable[type, SamplesPerEase];
                default:
                {
                    var sampleProgress = scaledProgress - index;
                    return EaseTable[type, index]
                           + sampleProgress * (EaseTable[type, index + 1] - EaseTable[type, index]);
                }
            }
        }

        /// <summary>
        /// 按原版规则计算指定缓动类型的缓动值。
        /// </summary>
        /// <param name="progress">事件进度，通常位于 0 到 1 之间。</param>
        /// <param name="type">缓动类型。</param>
        /// <returns>计算得到的缓动值。</returns>
        public static float GetEaseWithProgress(float progress, AreaEaseType type) =>
            GetEaseWithProgress(progress, (int)type);

        /// <summary>
        /// 根据缓动后的进度计算两个数值之间的插值。
        /// </summary>
        /// <param name="start">上一个关键帧的数值。</param>
        /// <param name="end">当前关键帧的目标数值。</param>
        /// <param name="progress">事件进度，通常位于 0 到 1 之间。</param>
        /// <param name="type">缓动类型。</param>
        /// <returns>插值结果。</returns>
        public static float Interpolate(
            float start,
            float end,
            float progress,
            AreaEaseType type
        )
        {
            var easedProgress = GetEaseWithProgress(progress, type);
            return start + (end - start) * easedProgress;
        }

        /// <summary>
        /// 根据前一事件时间、当前事件时间和查询时间计算事件进度。
        /// </summary>
        /// <param name="previousEventTime">前一个关键帧时间，单位为音乐时间秒。</param>
        /// <param name="eventTime">当前关键帧时间，单位为音乐时间秒。</param>
        /// <param name="time">要查询的音乐时间，单位为秒。</param>
        /// <returns>当前时间在两个关键帧之间的归一化进度。</returns>
        /// <exception cref="ArgumentOutOfRangeException">任一时间不是有限数值。</exception>
        public static float GetProgress(float previousEventTime, float eventTime, float time)
        {
            if (!IsFinite(previousEventTime))
                throw new ArgumentOutOfRangeException(
                    nameof(previousEventTime),
                    "时间必须是有限数值。"
                );
            if (!IsFinite(eventTime))
                throw new ArgumentOutOfRangeException(
                    nameof(eventTime),
                    "时间必须是有限数值。"
                );
            if (!IsFinite(time))
                throw new ArgumentOutOfRangeException(nameof(time), "时间必须是有限数值。");

            if (Math.Abs(previousEventTime - eventTime) < Common.CoreConstants.FloatEpsilon)
                return time < eventTime ? 0f : 1f;

            return (time - previousEventTime) / (eventTime - previousEventTime);
        }

        /// <summary>
        /// 对二维坐标或倍率分别按 x、y 方向的缓动类型进行插值。
        /// </summary>
        /// <param name="start">上一个关键帧的二维数值。</param>
        /// <param name="end">当前关键帧的目标二维数值。</param>
        /// <param name="progress">事件进度，通常位于 0 到 1 之间。</param>
        /// <param name="easeX">x 方向的缓动类型。</param>
        /// <param name="easeY">y 方向的缓动类型。</param>
        /// <returns>插值后的二维数值。</returns>
        public static PositionUnit Interpolate(
            PositionUnit start,
            PositionUnit end,
            float progress,
            AreaEase easeX,
            AreaEase easeY
        ) =>
            new()
            {
                X = easeX.Interpolate(start.X, end.X, progress),
                Y = easeY.Interpolate(start.Y, end.Y, progress),
            };

        private static float[,] CreateEaseTable()
        {
            var table = new float[EaseTypeCount, SamplesPerEase + 1];
            for (var type = 0; type < EaseTypeCount; type++)
            {
                for (var sample = 0; sample <= SamplesPerEase; sample++)
                {
                    var progress = sample / (float)SamplesPerEase;
                    table[type, sample] = CalculateEaseValue(type, progress);
                }
            }

            return table;
        }

        private static float CalculateEaseValue(int type, float progress)
        {
            return (float)(
                type switch
                {
                    (int)AreaEaseType.Linear => Easings.Linear(progress),
                    (int)AreaEaseType.EaseInQuad => Easings.EaseInQuad(progress),
                    (int)AreaEaseType.EaseOutQuad => Easings.EaseOutQuad(progress),
                    (int)AreaEaseType.EaseInOutQuad => Easings.EaseInOutQuad(progress),
                    (int)AreaEaseType.EaseInCubic => Easings.EaseInCubic(progress),
                    (int)AreaEaseType.EaseOutCubic => Easings.EaseOutCubic(progress),
                    (int)AreaEaseType.EaseInOutCubic => Easings.EaseInOutCubic(progress),
                    (int)AreaEaseType.EaseInQuart => Easings.EaseInQuart(progress),
                    (int)AreaEaseType.EaseOutQuart => Easings.EaseOutQuart(progress),
                    (int)AreaEaseType.EaseInOutQuart => Easings.EaseInOutQuart(progress),
                    (int)AreaEaseType.EaseInQuint => Easings.EaseInQuint(progress),
                    (int)AreaEaseType.EaseOutQuint => Easings.EaseOutQuint(progress),
                    (int)AreaEaseType.EaseInOutQuint => Easings.EaseInOutQuint(progress),
                    (int)AreaEaseType.Zero => 0d,
                    (int)AreaEaseType.One => 1d,
                    _ => throw new ArgumentOutOfRangeException(nameof(type)),
                }
            );
        }

        private static float Interpolate(float start, float end, float progress, int type)
        {
            var easedProgress = GetEaseWithProgress(progress, type);
            return start + (end - start) * easedProgress;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
