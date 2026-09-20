using System.Reflection;

namespace KaedePhi.Tool.Common;

/// <summary>
/// 提供 Control 默认值检查的通用方法。
/// </summary>
public static class ControlDefaultChecker
{
    // 使用 Constants.FloatEpsilon

    /// <summary>
    /// 检查 Control 列表是否与默认值完全一致。
    /// </summary>
    /// <typeparam name="T">Control 类型</typeparam>
    /// <param name="controls">要检查的 Control 列表</param>
    /// <returns>如果与默认值一致则返回 true</returns>
    public static bool IsDefaultControls<T>(List<T>? controls)
        where T : IrControls.ControlBase
    {
        var defaultControls = GetDefaultControls<T>();
        if (controls == null || controls.Count != defaultControls.Count)
            return false;

        for (var i = 0; i < defaultControls.Count; i++)
        {
            if (!AreControlsEqual(controls[i], defaultControls[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 获取指定 Control 类型的默认值列表。
    /// </summary>
    /// <typeparam name="T">Control 类型</typeparam>
    /// <returns>默认值列表</returns>
    private static List<T> GetDefaultControls<T>()
        where T : IrControls.ControlBase
    {
        var defaultProperty = typeof(T).GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.Static
        );
        if (defaultProperty == null)
            throw new InvalidOperationException(
                $"Type {typeof(T).Name} does not have a static Default property."
            );

        var value = defaultProperty.GetValue(null);
        if (value is List<T> defaultList)
            return defaultList;

        throw new InvalidOperationException(
            $"Type {typeof(T).Name}.Default is not of type List<{typeof(T).Name}>."
        );
    }

    /// <summary>
    /// 比较两个 Control 对象是否相等。
    /// </summary>
    /// <typeparam name="T">Control 类型</typeparam>
    /// <param name="a">第一个 Control</param>
    /// <param name="b">第二个 Control</param>
    /// <returns>如果相等则返回 true</returns>
    private static bool AreControlsEqual<T>(T a, T b)
        where T : IrControls.ControlBase
    {
        // 比较基类属性
        if ((int)a.Easing != (int)b.Easing)
            return false;
        if (Math.Abs(a.X - b.X) > Constants.FloatEpsilon)
            return false;

        // 比较派生类特定属性
        return AreDerivedPropertiesEqual(a, b);
    }

    /// <summary>
    /// 比较派生类特定属性。
    /// </summary>
    /// <typeparam name="T">Control 类型</typeparam>
    /// <param name="a">第一个 Control</param>
    /// <param name="b">第二个 Control</param>
    /// <returns>如果特定属性相等则返回 true</returns>
    private static bool AreDerivedPropertiesEqual<T>(T a, T b)
        where T : IrControls.ControlBase
    {
        var type = typeof(T);

        // 处理已知的 Control 类型
        if (type == typeof(IrControls.XControl))
        {
            var xa = (IrControls.XControl)(object)a;
            var xb = (IrControls.XControl)(object)b;
            return Math.Abs(xa.Pos - xb.Pos) <= Constants.FloatEpsilon;
        }

        if (type == typeof(IrControls.AlphaControl))
        {
            var aa = (IrControls.AlphaControl)(object)a;
            var ab = (IrControls.AlphaControl)(object)b;
            return Math.Abs(aa.Alpha - ab.Alpha) <= Constants.FloatEpsilon;
        }

        if (type == typeof(IrControls.SizeControl))
        {
            var sa = (IrControls.SizeControl)(object)a;
            var sb = (IrControls.SizeControl)(object)b;
            return Math.Abs(sa.Size - sb.Size) <= Constants.FloatEpsilon;
        }

        if (type == typeof(IrControls.SkewControl))
        {
            var ska = (IrControls.SkewControl)(object)a;
            var skb = (IrControls.SkewControl)(object)b;
            return Math.Abs(ska.Skew - skb.Skew) <= Constants.FloatEpsilon;
        }

        if (type == typeof(IrControls.YControl))
        {
            var ya = (IrControls.YControl)(object)a;
            var yb = (IrControls.YControl)(object)b;
            return Math.Abs(ya.Y - yb.Y) <= Constants.FloatEpsilon;
        }

        // 对于未知类型，使用反射比较所有属性
        return ArePropertiesEqualByReflection(a, b, type);
    }

    /// <summary>
    /// 使用反射比较两个对象的所有属性。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="a">第一个对象</param>
    /// <param name="b">第二个对象</param>
    /// <param name="type">对象类型</param>
    /// <returns>如果所有属性相等则返回 true</returns>
    private static bool ArePropertiesEqualByReflection<T>(T a, T b, Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanRead)
                continue;

            var valueA = prop.GetValue(a);
            var valueB = prop.GetValue(b);

            if (prop.PropertyType == typeof(float))
            {
                var floatA = (float)(valueA ?? 0f);
                var floatB = (float)(valueB ?? 0f);
                if (Math.Abs(floatA - floatB) > Constants.FloatEpsilon)
                    return false;
            }
            else if (prop.PropertyType == typeof(double))
            {
                var doubleA = (double)(valueA ?? 0d);
                var doubleB = (double)(valueB ?? 0d);
                if (Math.Abs(doubleA - doubleB) > Constants.FloatEpsilon)
                    return false;
            }
            else if (prop.PropertyType == typeof(int))
            {
                var intA = (int)(valueA ?? 0);
                var intB = (int)(valueB ?? 0);
                if (intA != intB)
                    return false;
            }
            else if (prop.PropertyType == typeof(bool))
            {
                var boolA = (bool)(valueA ?? false);
                var boolB = (bool)(valueB ?? false);
                if (boolA != boolB)
                    return false;
            }
            else if (prop.PropertyType == typeof(string))
            {
                var strA = valueA as string;
                var strB = valueB as string;
                if (strA != strB)
                    return false;
            }
            else if (
                (prop.PropertyType.IsEnum || prop.PropertyType.IsClass) && !Equals(valueA, valueB)
            )
                return false;
        }

        return true;
    }
}
