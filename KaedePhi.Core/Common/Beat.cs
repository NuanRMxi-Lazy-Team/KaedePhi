using System;
using KaedePhi.Core.Utils;
using Newtonsoft.Json;

namespace KaedePhi.Core.Common
{
    /// <summary>
    /// 标准节拍格式，表示为 beat[0]:beat[1]/beat[2]。
    /// 使用 float 或 double 隐式转换时，返回 CurBeat = beat[1] / beat[2] + beat[0]。
    /// 使用 int[] 隐式转换时，返回包含三个元素的新数组。
    /// </summary>
    [JsonConverter(typeof(BeatJsonConverter))]
    public readonly struct Beat : IComparable<Beat>, IEquatable<Beat>
    {
        private readonly int _whole;
        private readonly int _numerator;
        private readonly int _denominator;
        private readonly double _curBeatDouble;
        private readonly float _curBeatFloat;

        private int EffectiveDenominator => _denominator == 0 ? 1 : _denominator;

        public Beat(int[] beatArray)
        {
            if (beatArray is null)
                throw new ArgumentNullException(nameof(beatArray));
            if (beatArray.Length != 3)
                throw new ArgumentException(
                    "Beat array must have exactly 3 elements.",
                    nameof(beatArray)
                );
            if (beatArray[2] == 0)
                throw new ArgumentException(
                    "Beat denominator (beat[2]) cannot be zero.",
                    nameof(beatArray)
                );
            if (beatArray[2] < 0)
                throw new ArgumentException(
                    "Beat denominator (beat[2]) must be positive.",
                    nameof(beatArray)
                );

            _whole = beatArray[0];
            _numerator = beatArray[1];
            _denominator = beatArray[2];
            _curBeatDouble = (double)_numerator / _denominator + _whole;
            _curBeatFloat = (float)_curBeatDouble;
        }

        public Beat(double beat)
        {
            if (double.IsNaN(beat) || double.IsInfinity(beat))
                throw new ArgumentOutOfRangeException(nameof(beat), "Beat must be finite.");
            if (beat is < int.MinValue or > int.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(beat),
                    "Beat must fit in the supported integer part range."
                );

            _curBeatDouble = beat;
            _curBeatFloat = (float)beat;

            var wholePart = (int)Math.Floor(beat);
            var fractionalPart = beat - wholePart;

            if (Math.Abs(fractionalPart) < 1e-9)
            {
                _whole = wholePart;
                _numerator = 0;
                _denominator = 1;
                return;
            }

            int numerator = 1,
                denominator = 0;
            int prevNumerator = 0,
                prevDenominator = 1;
            var remaining = fractionalPart;
            const int maxDenominator = 1000;

            for (var iteration = 0; iteration < 20; iteration++)
            {
                var digit = (int)Math.Floor(remaining);

                var tempNum = digit * numerator + prevNumerator;
                var tempDen = digit * denominator + prevDenominator;

                if (tempDen > maxDenominator)
                    break;

                prevNumerator = numerator;
                prevDenominator = denominator;
                numerator = tempNum;
                denominator = tempDen;

                remaining -= digit;
                if (
                    Math.Abs(remaining) < 1e-9
                    || Math.Abs((double)numerator / denominator - fractionalPart) < 1e-9
                )
                    break;

                remaining = 1.0 / remaining;
            }

            if (denominator == 0)
            {
                denominator = 1000;
                numerator = (int)Math.Round(fractionalPart * denominator);
                var gcd = Gcd(numerator, denominator);
                numerator /= gcd;
                denominator /= gcd;
            }

            _whole = wholePart;
            _numerator = numerator;
            _denominator = denominator;
        }

        private Beat(int whole, int numerator, int denominator)
        {
            _whole = whole;
            _numerator = numerator;
            _denominator = denominator;
            _curBeatDouble = (double)numerator / denominator + whole;
            _curBeatFloat = (float)_curBeatDouble;
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }

            return a;
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }

            return a;
        }

        public int this[int index] =>
            index switch
            {
                0 => _whole,
                1 => _numerator,
                2 => EffectiveDenominator,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Beat index must be between 0 and 2."
                ),
            };

        // 隐式转换为 float
        public static implicit operator float(Beat beat) => beat._curBeatFloat;

        // 隐式转换为 double
        public static implicit operator double(Beat beat) => beat._curBeatDouble;

        // 隐式转换为 int[]，返回新数组
        public static implicit operator int[](Beat beat) =>
            new[] { beat._whole, beat._numerator, beat.EffectiveDenominator };

        public static Beat operator +(Beat a, Beat b)
        {
            var wholePart = a._whole + b._whole;

            var numerator =
                (long)a._numerator * b.EffectiveDenominator
                + (long)b._numerator * a.EffectiveDenominator;
            var denominator = (long)a.EffectiveDenominator * b.EffectiveDenominator;

            if (numerator >= denominator)
            {
                var carry = numerator / denominator;
                wholePart += (int)carry;
                numerator %= denominator;
            }

            if (numerator < 0)
            {
                var borrowCount = (-numerator + denominator - 1) / denominator;
                wholePart -= (int)borrowCount;
                numerator += borrowCount * denominator;
            }

            if (numerator == 0)
                return new Beat(wholePart, 0, 1);

            var gcd = Gcd(Math.Abs(numerator), denominator);
            numerator /= gcd;
            denominator /= gcd;

            if (numerator > int.MaxValue || denominator > int.MaxValue)
                throw new OverflowException(
                    "Beat calculation resulted in values too large for int representation."
                );

            return new Beat(wholePart, (int)numerator, (int)denominator);
        }

        public static Beat operator -(Beat a, Beat b)
        {
            var wholePart = a._whole - b._whole;

            var numerator =
                (long)a._numerator * b.EffectiveDenominator
                - (long)b._numerator * a.EffectiveDenominator;
            var denominator = (long)a.EffectiveDenominator * b.EffectiveDenominator;

            if (numerator < 0)
            {
                var borrowCount = (-numerator + denominator - 1) / denominator;
                wholePart -= (int)borrowCount;
                numerator += borrowCount * denominator;
            }

            if (numerator == 0)
                return new Beat(wholePart, 0, 1);

            var gcd = Gcd(Math.Abs(numerator), denominator);
            numerator /= gcd;
            denominator /= gcd;

            if (numerator > int.MaxValue || denominator > int.MaxValue)
                throw new OverflowException(
                    "Beat calculation resulted in values too large for int representation."
                );

            return new Beat(wholePart, (int)numerator, (int)denominator);
        }

        public static bool operator <(Beat a, Beat b) => a._curBeatDouble < b._curBeatDouble;

        public static bool operator >(Beat a, Beat b) => a._curBeatDouble > b._curBeatDouble;

        public static bool operator <=(Beat a, Beat b) => a._curBeatDouble <= b._curBeatDouble;

        public static bool operator >=(Beat a, Beat b) => a._curBeatDouble >= b._curBeatDouble;

        public static bool operator ==(Beat a, Beat b) => a._curBeatDouble.Equals(b._curBeatDouble);

        public static bool operator !=(Beat a, Beat b) =>
            !a._curBeatDouble.Equals(b._curBeatDouble);

        public override string ToString() => $"{_whole}:{_numerator}/{EffectiveDenominator}";

        public override bool Equals(object? obj) => obj is Beat other && Equals(other);

        public bool Equals(Beat other) => _curBeatDouble.Equals(other._curBeatDouble);

        public override int GetHashCode() => _curBeatDouble.GetHashCode();

        public int CompareTo(Beat other) => _curBeatDouble.CompareTo(other._curBeatDouble);
    }
}
