using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KaedePhi.Core.Common;
using KaedePhi.Core.Utils;

namespace KaedePhi.Core.Kpc
{
    public class Event<T>
    {
        /// <summary>
        /// 是否为贝塞尔曲线
        /// </summary>
        public bool IsBezier { get; set; } // 是否为贝塞尔曲线

        /// <summary>
        /// 贝塞尔曲线控制点
        /// </summary>
        public float[] BezierPoints { get; set; } = new float[4]; // 贝塞尔曲线点

        /// <summary>
        /// 缓动截取左界限
        /// </summary>
        public float EasingLeft { get; set; } // 缓动开始

        /// <summary>
        /// 缓动截取右界限
        /// </summary>
        public float EasingRight { get; set; } = 1.0f; // 缓动结束

        /// <summary>
        /// 缓动类型
        /// </summary>
        public Easing Easing { get; set; } = new(1); // 缓动类型

        /// <summary>
        /// 事件开始数值
        /// </summary>
        public T StartValue { get; set; } // 开始值

        /// <summary>
        /// 事件结束数值
        /// </summary>
        public T EndValue { get; set; } // 结束值

        /// <summary>
        /// 事件开始拍
        /// </summary>
        public Beat StartBeat { get; set; } = new(new[] { 0, 0, 1 }); // 开始时间

        /// <summary>
        /// 事件结束拍
        /// </summary>
        public Beat EndBeat { get; set; } = new(new[] { 1, 0, 1 }); // 结束时间

        /// <summary>
        /// 当此事件为文字事件时，此值为字体文件相对路径，默认cmdysj.ttf
        /// </summary>
#nullable enable
        public string? Font { get; set; } = null;
#nullable disable
        /// <summary>
        /// 获取某个拍在这个事件中的值
        /// </summary>
        /// <param name="beat">指定拍</param>
        /// <returns>指定拍时，此事件的数值</returns>
        public T GetValueAtBeat(Beat beat)
        {
            var t = (beat - StartBeat) / (EndBeat - StartBeat);
            if (t <= 0)
                return StartValue;
            if (t >= 1)
                return EndValue;

            // 如果启用了贝塞尔曲线,使用 Bezier.Do
            if (IsBezier)
            {
                // 检查 T 的类型并调用相应的方法
                if (typeof(T) == typeof(float))
                    return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToSingle(StartValue),
                        Convert.ToSingle(EndValue), EasingLeft, EasingRight);
                else if (typeof(T) == typeof(double))
                    return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToDouble(StartValue),
                        Convert.ToDouble(EndValue), EasingLeft, EasingRight);
                else if (typeof(T) == typeof(int))
                    return (T)(object)Bezier.Do(BezierPoints, t, Convert.ToInt32(StartValue),
                        Convert.ToInt32(EndValue), EasingLeft, EasingRight);
                else if (typeof(T) == typeof(byte[]))
                {
                    byte[] startBytes = StartValue as byte[];
                    byte[] endBytes = EndValue as byte[];
                    if (startBytes == null || endBytes == null)
                        throw new InvalidOperationException("Start or End is not a byte array, or is null.");
                    if (startBytes.Length != endBytes.Length)
                        throw new InvalidOperationException(
                            "Byte arrays must be of the same length for interpolation.");
                    byte[] result = new byte[startBytes.Length];
                    for (int i = 0; i < startBytes.Length; i++)
                        result[i] = Bezier.Do(BezierPoints, t, startBytes[i], endBytes[i], EasingLeft, EasingRight);
                    return (T)(object)result;
                }
                else
                    throw new NotSupportedException($"类型 {typeof(T)} 不受支持。");
            }

            // 检查 T 的类型并调用相应的方法
            if (typeof(T) == typeof(float))
                return (T)(object)Easing.Interpolate(EasingLeft, EasingRight, Convert.ToSingle(StartValue),
                    Convert.ToSingle(EndValue),
                    t);
            else if (typeof(T) == typeof(double))
                return (T)(object)Easing.Interpolate(EasingLeft, EasingRight, Convert.ToDouble(StartValue),
                    Convert.ToDouble(EndValue),
                    t);
            else if (typeof(T) == typeof(int))
                return (T)(object)Easing.Interpolate(EasingLeft, EasingRight, Convert.ToInt32(StartValue),
                    Convert.ToInt32(EndValue), t);
            else if (typeof(T) == typeof(byte[]))
            {
                byte[] startBytes = StartValue as byte[];
                byte[] endBytes = EndValue as byte[];
                if (startBytes == null || endBytes == null)
                    throw new InvalidOperationException("Start or End is not a byte array, or is null.");
                if (startBytes.Length != endBytes.Length)
                    throw new InvalidOperationException(
                        "Byte arrays must be of the same length for interpolation.");
                byte[] result = new byte[startBytes.Length];
                for (int i = 0; i < startBytes.Length; i++)
                    result[i] = Easing.Interpolate(EasingLeft, EasingRight, startBytes[i], endBytes[i], t);
                return (T)(object)result;
            }
            else
                throw new NotSupportedException($"类型 {typeof(T)} 不受支持。");
        }

        // 各种反射结果缓存，static per Event<T> closed type
        private static readonly MethodInfo DeepCloneGenericDef =
            typeof(Event<T>).GetMethod("DeepClone", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Dictionary<Type, bool> _immutableCache = new();
        private static readonly Dictionary<Type, MethodInfo> _deepCloneMethodCache = new();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldsCache = new();
        private static readonly Dictionary<Type, MethodInfo> _typeCloneMethodCache = new();

        private static bool IsImmutableType(Type type)
        {
            if (_immutableCache.TryGetValue(type, out var cached))
                return cached;

            var result = type.IsValueType || type.IsEnum ||
                         type == typeof(string) ||
                         type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                         type == typeof(TimeSpan) || type == typeof(Guid) ||
                         type == typeof(Uri) || type == typeof(Version);

            _immutableCache[type] = result;
            return result;
        }

        private MethodInfo GetDeepCloneMethod(Type type)
        {
            if (!_deepCloneMethodCache.TryGetValue(type, out var method))
            {
                method = DeepCloneGenericDef.MakeGenericMethod(type);
                _deepCloneMethodCache[type] = method;
            }
            return method;
        }

        private TValue DeepClone<TValue>(TValue value)
        {
            if (value == null)
                return default;

            // 快速路径：直接处理常见基础类型，避免反射开销和 GC
            if (value is int or float or double or long or short or byte or sbyte or
                uint or ulong or ushort or char or bool or decimal or
                string or DateTime or DateTimeOffset or TimeSpan or Guid)
                return value;

            // 快速路径：byte[]
            if (value is byte[] byteArr)
                return (TValue)(object)byteArr.ToArray();

            // 快速路径：float[]
            if (value is float[] floatArr)
                return (TValue)(object)floatArr.ToArray();

            var type = typeof(TValue);

            if (IsImmutableType(type))
                return value;

            // 处理数组
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var array = value as Array;
                var clonedArray = Array.CreateInstance(elementType, array.Length);
                if (IsImmutableType(elementType))
                {
                    Array.Copy(array, clonedArray, array.Length);
                }
                else
                {
                    var cloneMethod = GetDeepCloneMethod(elementType);
                    for (int i = 0; i < array.Length; i++)
                        clonedArray.SetValue(cloneMethod.Invoke(this, new[] { array.GetValue(i) }), i);
                }
                return (TValue)(object)clonedArray;
            }

            // 处理泛型集合 (List<T>, Dictionary<K,V>, etc.)
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var list = (System.Collections.IList)value;
                var clonedList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                if (IsImmutableType(elementType))
                {
                    foreach (var item in list) clonedList.Add(item);
                }
                else
                {
                    var cloneMethod = GetDeepCloneMethod(elementType);
                    foreach (var item in list)
                        clonedList.Add(cloneMethod.Invoke(this, new[] { item }));
                }
                return (TValue)clonedList;
            }

            // 尝试调用对象的 Clone() 方法
            if (!_typeCloneMethodCache.TryGetValue(type, out var cloneMethodInfo))
            {
                var m = type.GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                cloneMethodInfo = m?.ReturnType == type ? m : null;
                _typeCloneMethodCache[type] = cloneMethodInfo;
            }
            if (cloneMethodInfo != null)
                return (TValue)cloneMethodInfo.Invoke(value, null);

            // 使用反射进行浅拷贝并递归处理引用类型字段
            if (!_fieldsCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _fieldsCache[type] = fields;
            }

            var cloned = Activator.CreateInstance(type);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(value);
                if (fieldValue == null) continue;
                var fieldType = field.FieldType;
                if (IsImmutableType(fieldType))
                    field.SetValue(cloned, fieldValue);
                else
                    field.SetValue(cloned, GetDeepCloneMethod(fieldType).Invoke(this, new[] { fieldValue }));
            }

            return (TValue)cloned;
        }

        public Event<T> Clone()
        {
            return new Event<T>
            {
                IsBezier = IsBezier,
                BezierPoints = BezierPoints.ToArray(),
                EasingLeft = EasingLeft,
                EasingRight = EasingRight,
                Easing = Easing,
                StartValue = DeepClone(StartValue),
                EndValue = DeepClone(EndValue),
                StartBeat = new Beat((int[])StartBeat),
                EndBeat = new Beat((int[])EndBeat),
                Font = Font
            };
        }
    }
}