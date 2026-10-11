namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 缓存曲线在单次几何采样中的计算结果。
/// </summary>
/// <param name="curveCount">可缓存的曲线数量</param>
public sealed class StellateCurveValueCache(int curveCount)
{
    private readonly double[] _values = new double[curveCount];
    private readonly int[] _generations = new int[curveCount];
    private int _generation = 1;

    /// <summary>
    /// 开始一个新的采样周期。
    /// </summary>
    public void BeginSample()
    {
        if (_generation == int.MaxValue)
        {
            Array.Clear(_generations);
            _generation = 1;
        }
        else
        {
            _generation++;
        }
    }

    /// <summary>
    /// 尝试获取曲线在当前采样周期中的缓存值。
    /// </summary>
    /// <param name="curve">待查询的曲线索引</param>
    /// <param name="value">找到缓存值时接收该值</param>
    /// <returns>存在当前周期缓存值时为 <see langword="true"/>，否则为 <see langword="false"/></returns>
    public bool TryGetValue(StellateCurveIndex curve, out double value)
    {
        var index = curve.CacheIndex;
        if (_generations[index] == _generation)
        {
            value = _values[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 保存曲线在当前采样周期中的计算值。
    /// </summary>
    /// <param name="curve">曲线索引</param>
    /// <param name="value">待保存的曲线值</param>
    public void SetValue(StellateCurveIndex curve, double value)
    {
        var index = curve.CacheIndex;
        _values[index] = value;
        _generations[index] = _generation;
    }
}
