namespace KaedePhi.Tool.Common;

/// <summary>
/// 坐标几何工具类，提供 Kpc 坐标系与其他坐标系之间的互转、角度映射、旋转变换及屏幕空间距离计算等功能。
/// </summary>
/// <remarks>
/// <para>
/// <b>坐标系约定：</b><br/>
/// Kpc（KaedePhi Chart）是内部统一的归一化坐标空间，X 轴范围 [-1, 1]，Y 轴范围 [-1, 1]。
/// 所有坐标转换以 Kpc 为中介格式，源格式坐标先映射到 Kpc，再从 Kpc 映射到目标格式坐标。
/// </para>
/// <para>
/// <b>旋转变换约定：</b><br/>
/// 判定线偏移旋转在物理等比空间（以半宽为单位）中执行，以匹配 RPE 引擎（prpr）的父子坐标变换语义：
/// 旋转前将 Kpc Y 乘以 <c>halfH/halfW</c> 折算到物理等比坐标，旋转后除以该比例还原。
/// 角度正方向为逆时针（CCW），与 Kpc 内部约定一致。
/// </para>
/// <para>
/// <b>屏幕空间几何：</b><br/>
/// 距离和模长的计算在渲染坐标系（render profile）中进行，以反映实际屏幕上的几何感知。
/// 旋转计算与屏幕空间几何评估相互独立。
/// </para>
/// </remarks>
internal static class CoordinateGeometry
{
    private static readonly CoordinateProfile KpcProfile = CoordinateProfile.KpcProfile;

    private static readonly CoordinateProfile RenderProfileDefault =
        CoordinateProfile.DefaultRenderProfile;

    /// <summary>
    /// 计算坐标轴跨度，并校验跨度不为零。
    /// </summary>
    /// <param name="min">坐标轴最小值。</param>
    /// <param name="max">坐标轴最大值。</param>
    /// <param name="axisName">轴名称，仅用于异常消息定位。</param>
    /// <returns>坐标轴跨度（<paramref name="max"/> - <paramref name="min"/>）。</returns>
    /// <exception cref="InvalidOperationException">
    /// 当 <paramref name="max"/> 与 <paramref name="min"/> 之差的绝对值小于 <c>1e-12</c> 时抛出。
    /// </exception>
    private static double GetSpan(double min, double max, string axisName)
    {
        var span = max - min;
        return Math.Abs(span) < 1e-12
            ? throw new InvalidOperationException($"Coordinate span of axis '{axisName}' is zero.")
            : span;
    }

    /// <summary>
    /// 将绝对坐标值从源区间线性映射到目标区间。
    /// </summary>
    /// <param name="value">待映射的源坐标值。</param>
    /// <param name="sourceMin">源区间最小值。</param>
    /// <param name="sourceMax">源区间最大值。</param>
    /// <param name="targetMin">目标区间最小值。</param>
    /// <param name="targetMax">目标区间最大值。</param>
    /// <param name="axisName">轴名称，传递给 <see cref="GetSpan"/> 用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标值。</returns>
    private static double MapValue(
        double value,
        double sourceMin,
        double sourceMax,
        double targetMin,
        double targetMax,
        string axisName
    )
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return targetMin + (value - sourceMin) / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将坐标增量（偏移量）从源区间按比例缩放到目标区间，不做原点平移。
    /// </summary>
    /// <param name="delta">待映射的源坐标增量。</param>
    /// <param name="sourceMin">源区间最小值，与 <paramref name="sourceMax"/> 一起用于计算源跨度。</param>
    /// <param name="sourceMax">源区间最大值。</param>
    /// <param name="targetMin">目标区间最小值，与 <paramref name="targetMax"/> 一起用于计算目标跨度。</param>
    /// <param name="targetMax">目标区间最大值。</param>
    /// <param name="axisName">轴名称，传递给 <see cref="GetSpan"/> 用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标增量（<c>delta * targetSpan / sourceSpan</c>）。</returns>
    private static double MapDelta(
        double delta,
        double sourceMin,
        double sourceMax,
        double targetMin,
        double targetMax,
        string axisName
    )
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return delta / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将 Kpc X 坐标映射到目标坐标系 X 坐标（内部实现）。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    private static double ToTargetXCore(double x, CoordinateProfile target) =>
        MapValue(x, KpcProfile.MinX, KpcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 Kpc Y 坐标映射到目标坐标系 Y 坐标（内部实现）。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    private static double ToTargetYCore(double y, CoordinateProfile target) =>
        MapValue(y, KpcProfile.MinY, KpcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将源坐标系 X 坐标映射到 Kpc X 坐标（内部实现）。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 坐标系下的 X 坐标。</returns>
    private static double ToKpcXCore(double x, CoordinateProfile source) =>
        MapValue(x, source.MinX, source.MaxX, KpcProfile.MinX, KpcProfile.MaxX, "X");

    /// <summary>
    /// 将源坐标系 Y 坐标映射到 Kpc Y 坐标（内部实现）。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 坐标系下的 Y 坐标。</returns>
    private static double ToKpcYCore(double y, CoordinateProfile source) =>
        MapValue(y, source.MinY, source.MaxY, KpcProfile.MinY, KpcProfile.MaxY, "Y");

    /// <summary>
    /// 将 Kpc X 增量按目标坐标系比例缩放（内部实现），不做原点平移。
    /// </summary>
    /// <param name="x">Kpc X 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 增量。</returns>
    private static double ToTargetDeltaXCore(double x, CoordinateProfile target) =>
        MapDelta(x, KpcProfile.MinX, KpcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 Kpc Y 增量按目标坐标系比例缩放（内部实现），不做原点平移。
    /// </summary>
    /// <param name="y">Kpc Y 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 增量。</returns>
    private static double ToTargetDeltaYCore(double y, CoordinateProfile target) =>
        MapDelta(y, KpcProfile.MinY, KpcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将 Kpc 角度转换到目标坐标系角度（内部实现）。
    /// </summary>
    /// <param name="kpcAngleDegrees">Kpc 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>
    /// 当目标坐标系旋转方向与 Kpc 一致时返回原值；否则取反，以适配不同旋转正方向约定。
    /// </returns>
    private static double ToTargetAngleCore(double kpcAngleDegrees, CoordinateProfile target) =>
        target.ClockwiseRotation == KpcProfile.ClockwiseRotation
            ? kpcAngleDegrees
            : -kpcAngleDegrees;

    /// <summary>
    /// 将源坐标系角度转换到 Kpc 角度（内部实现）。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>
    /// 当源坐标系旋转方向与 Kpc 一致时返回原值；否则取反，以适配不同旋转正方向约定。
    /// </returns>
    private static double ToKpcAngleCore(double sourceAngleDegrees, CoordinateProfile source) =>
        source.ClockwiseRotation == KpcProfile.ClockwiseRotation
            ? sourceAngleDegrees
            : -sourceAngleDegrees;

    /// <summary>
    /// 将 Kpc X 坐标转换为指定坐标系的 X 坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    internal static double ToTargetX(double x, in CoordinateProfile target) =>
        ToTargetXCore(x, target);

    /// <summary>
    /// 将 Kpc Y 坐标转换为指定坐标系的 Y 坐标。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    internal static double ToTargetY(double y, in CoordinateProfile target) =>
        ToTargetYCore(y, target);

    /// <summary>
    /// 将 Kpc X 坐标转换为指定坐标系的 X 坐标（单精度浮点数）。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标（<see langword="float"/>）。</returns>
    internal static float ToTargetXf(double x, in CoordinateProfile target) =>
        (float)ToTargetXCore(x, target);

    /// <summary>
    /// 将 Kpc Y 坐标转换为指定坐标系的 Y 坐标（单精度浮点数）。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标（<see langword="float"/>）。</returns>
    internal static float ToTargetYf(double y, in CoordinateProfile target) =>
        (float)ToTargetYCore(y, target);

    /// <summary>
    /// 将指定坐标系的 X 坐标转换为 Kpc X 坐标。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc X 坐标。</returns>
    internal static double ToKpcX(double x, in CoordinateProfile source) => ToKpcXCore(x, source);

    /// <summary>
    /// 将默认渲染坐标系的 X 坐标转换为 Kpc X 坐标。
    /// </summary>
    /// <param name="x">默认渲染坐标系 X 坐标。</param>
    /// <returns>Kpc X 坐标。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToKpcX(double x) => ToKpcXCore(x, RenderProfileDefault);

    /// <summary>
    /// 将指定坐标系的 Y 坐标转换为 Kpc Y 坐标。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc Y 坐标。</returns>
    internal static double ToKpcY(double y, in CoordinateProfile source) => ToKpcYCore(y, source);

    /// <summary>
    /// 将默认渲染坐标系的 Y 坐标转换为 Kpc Y 坐标。
    /// </summary>
    /// <param name="y">默认渲染坐标系 Y 坐标。</param>
    /// <returns>Kpc Y 坐标。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToKpcY(double y) => ToKpcYCore(y, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc 角度转换为指定坐标系的角度。
    /// </summary>
    /// <param name="kpcAngleDegrees">Kpc 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的角度（度）。</returns>
    /// <remarks>
    /// 当目标坐标系与 Kpc 旋转正方向一致时角度值不变；否则取反，以匹配目标坐标系的旋转约定。
    /// </remarks>
    internal static double ToTargetAngle(double kpcAngleDegrees, in CoordinateProfile target) =>
        ToTargetAngleCore(kpcAngleDegrees, target);

    /// <summary>
    /// 将指定坐标系的角度转换为 Kpc 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 角度（度）。</returns>
    /// <remarks>
    /// 当源坐标系与 Kpc 旋转正方向一致时角度值不变；否则取反，以匹配 Kpc 的旋转约定。
    /// </remarks>
    internal static double ToKpcAngle(double sourceAngleDegrees, in CoordinateProfile source) =>
        ToKpcAngleCore(sourceAngleDegrees, source);

    /// <summary>
    /// 将默认渲染坐标系的角度转换为 Kpc 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">默认渲染坐标系角度（度）。</param>
    /// <returns>Kpc 角度（度）。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToKpcAngle(double sourceAngleDegrees) =>
        ToKpcAngleCore(sourceAngleDegrees, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc X 坐标转换为默认渲染坐标系的 X 坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <returns>默认渲染坐标系下的 X 坐标。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToRenderX(double x) => ToTargetXCore(x, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc Y 坐标转换为默认渲染坐标系的 Y 坐标。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系下的 Y 坐标。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToRenderY(double y) => ToTargetYCore(y, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc X 坐标转换为默认渲染坐标系的 X 坐标（单精度浮点数）。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <returns>默认渲染坐标系下的 X 坐标（<see langword="float"/>）。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static float ToRenderXf(double x) => (float)ToRenderX(x);

    /// <summary>
    /// 将 Kpc Y 坐标转换为默认渲染坐标系的 Y 坐标（单精度浮点数）。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系下的 Y 坐标（<see langword="float"/>）。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static float ToRenderYf(double y) => (float)ToRenderY(y);

    /// <summary>
    /// 将 Kpc 角度转换为默认渲染坐标系的角度。
    /// </summary>
    /// <param name="kpcAngleDegrees">Kpc 角度（度）。</param>
    /// <returns>默认渲染坐标系下的角度（度）。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double ToRenderAngle(double kpcAngleDegrees) =>
        ToTargetAngleCore(kpcAngleDegrees, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc 点坐标转换为默认渲染坐标系的点坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系下的点坐标 <c>(X, Y)</c>。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static (double X, double Y) ToRenderPoint(double x, double y) =>
        (ToRenderX(x), ToRenderY(y));

    /// <summary>
    /// 将 Kpc 点坐标转换为指定坐标系的点坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的点坐标 <c>(X, Y)</c>。</returns>
    private static (double X, double Y) ToTargetPoint(
        double x,
        double y,
        in CoordinateProfile target
    ) => (ToTargetXCore(x, target), ToTargetYCore(y, target));

    /// <summary>
    /// 在物理等比空间中旋转 Kpc 偏移向量，旋转后还原到 Kpc 坐标（指定渲染配置）。
    /// </summary>
    /// <param name="x">Kpc X 增量（偏移分量）。</param>
    /// <param name="y">Kpc Y 增量（偏移分量）。</param>
    /// <param name="angleDegrees">旋转角度（度），逆时针（CCW）为正，与 Kpc 内部约定一致。</param>
    /// <param name="renderProfile">渲染坐标配置，用于从 <c>SpanX</c> 和 <c>SpanY</c> 计算物理宽高比。</param>
    /// <returns>旋转后的 Kpc 增量向量 <c>(X, Y)</c>。</returns>
    /// <remarks>
    /// <para>
    /// RPE 引擎（prpr）的父子坐标变换在物理等比空间（以半宽 <c>halfW</c> 为单位）中执行。
    /// Kpc 归一化坐标中 X 以 <c>halfW</c> 为单位（正确），Y 以 <c>halfH</c> 为单位（<c>halfH ≠ halfW</c>），
    /// 因此旋转前需将 Kpc Y 乘以缩放因子 <c>k = halfH / halfW = SpanY / SpanX</c>，
    /// 折算到物理等比坐标后执行标准二维旋转，旋转完成后再将 Y 分量除以 <c>k</c> 还原到 Kpc 空间。
    /// </para>
    /// <para>
    /// 等价变换公式：
    /// <code>
    /// rotX = x * cos(θ) - y * k * sin(θ)
    /// rotY = (x * sin(θ) + y * k * cos(θ)) / k
    /// </code>
    /// 其中 <c>k = SpanY / SpanX</c>（典型值：900 / 1350 = 2/3）。
    /// </para>
    /// <para>
    /// 屏幕空间几何计算（误差阈值、距离评估）由
    /// <see cref="GetKpcScreenDistance(System.ValueTuple{double,double},System.ValueTuple{double,double},in CoordinateProfile)"/>
    /// 和 <see cref="GetKpcScreenMagnitude(System.ValueTuple{double,double},in CoordinateProfile)"/>
    /// 负责，与本方法的旋转计算相互独立。
    /// </para>
    /// </remarks>
    private static (double X, double Y) RotateKpcOffset(
        double x,
        double y,
        double angleDegrees,
        in CoordinateProfile renderProfile
    )
    {
        var spanX = GetSpan(renderProfile.MinX, renderProfile.MaxX, "X");
        var spanY = GetSpan(renderProfile.MinY, renderProfile.MaxY, "Y");
        var k = spanY / spanX;
        var rad = angleDegrees * (Math.PI / 180d);
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        return (x * cos - y * k * sin, (x * sin + y * k * cos) / k);
    }

    /// <summary>
    /// 在物理等比空间中旋转 Kpc 偏移向量，旋转后还原到 Kpc 坐标（使用默认渲染配置）。
    /// </summary>
    /// <param name="x">Kpc X 增量（偏移分量）。</param>
    /// <param name="y">Kpc Y 增量（偏移分量）。</param>
    /// <param name="angleDegrees">旋转角度（度），逆时针（CCW）为正。</param>
    /// <returns>旋转后的 Kpc 增量向量 <c>(X, Y)</c>。</returns>
    /// <remarks>
    /// 使用 <see cref="CoordinateProfile.DefaultRenderProfile"/> 作为宽高比参考。
    /// 旋转语义详见
    /// <see cref="RotateKpcOffset(double,double,double,in CoordinateProfile)"/>。
    /// </remarks>
    internal static (double X, double Y) RotateKpcOffset(double x, double y, double angleDegrees) =>
        RotateKpcOffset(x, y, angleDegrees, RenderProfileDefault);

    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在 Kpc 坐标系下的绝对位置（使用默认渲染配置）。
    /// </summary>
    /// <param name="fatherLineX">父线 Kpc X 坐标。</param>
    /// <param name="fatherLineY">父线 Kpc Y 坐标。</param>
    /// <param name="angleDegrees">旋转角度（度），逆时针（CCW）为正，与 Kpc 内部约定一致。</param>
    /// <param name="lineX">子线相对于父线原点的 Kpc X 偏移量。</param>
    /// <param name="lineY">子线相对于父线原点的 Kpc Y 偏移量。</param>
    /// <returns>子线在 Kpc 坐标系下的绝对位置 <c>(X, Y)</c>。</returns>
    /// <remarks>
    /// 旋转在物理等比空间内进行（X 与 Y 轴先折算到以半宽为单位的等比坐标），
    /// 以匹配 RPE 引擎（prpr）的父子坐标变换语义，确保解绑后子线在屏幕上的位置与原谱面一致。
    /// 旋转语义详见 <see cref="RotateKpcOffset(double,double,double)"/>。
    /// </remarks>
    internal static (double X, double Y) GetKpcAbsolutePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY
    )
    {
        var (rotX, rotY) = RotateKpcOffset(lineX, lineY, angleDegrees);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 根据父线位置与旋转角度，计算子线在 Kpc 坐标系下的绝对位置（指定渲染配置）。
    /// </summary>
    /// <param name="fatherLineX">父线 Kpc X 坐标。</param>
    /// <param name="fatherLineY">父线 Kpc Y 坐标。</param>
    /// <param name="angleDegrees">旋转角度（度），逆时针（CCW）为正，与 Kpc 内部约定一致。</param>
    /// <param name="lineX">子线相对于父线原点的 Kpc X 偏移量。</param>
    /// <param name="lineY">子线相对于父线原点的 Kpc Y 偏移量。</param>
    /// <param name="renderProfile">渲染坐标配置，用于从宽高比计算物理等比折算系数。</param>
    /// <returns>子线在 Kpc 坐标系下的绝对位置 <c>(X, Y)</c>。</returns>
    /// <remarks>
    /// <paramref name="renderProfile"/> 的宽高比决定了物理等比空间的缩放因子，
    /// 应与实际渲染分辨率保持一致，以保证解绑后子线在屏幕上的位置与原谱面一致。
    /// 旋转语义详见 <see cref="RotateKpcOffset(double,double,double,in CoordinateProfile)"/>。
    /// </remarks>
    internal static (double X, double Y) GetKpcAbsolutePos(
        double fatherLineX,
        double fatherLineY,
        double angleDegrees,
        double lineX,
        double lineY,
        in CoordinateProfile renderProfile
    )
    {
        var (rotX, rotY) = RotateKpcOffset(lineX, lineY, angleDegrees, renderProfile);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 计算 Kpc 点在默认渲染坐标系中距原点的欧氏距离（模长）。
    /// </summary>
    /// <param name="point">Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <returns>该点映射到默认渲染坐标系后，距渲染原点的欧氏距离。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double GetKpcScreenMagnitude((double X, double Y) point) =>
        GetKpcScreenMagnitude(point, RenderProfileDefault);

    /// <summary>
    /// 计算 Kpc 点在指定渲染坐标系中距原点的欧氏距离（模长）。
    /// </summary>
    /// <param name="point">Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <param name="renderProfile">渲染坐标配置，用于将 Kpc 点映射到渲染空间。</param>
    /// <returns>该点映射到 <paramref name="renderProfile"/> 坐标系后，距渲染原点的欧氏距离。</returns>
    /// <remarks>
    /// 结果反映该点在屏幕空间中的几何模长，可用于距离阈值判断。
    /// 注意：此为点到渲染原点的距离，而非两点间距离；两点间距离请使用
    /// <see cref="GetKpcScreenDistance(System.ValueTuple{double,double},System.ValueTuple{double,double},in CoordinateProfile)"/>。
    /// </remarks>
    private static double GetKpcScreenMagnitude(
        (double X, double Y) point,
        in CoordinateProfile renderProfile
    )
    {
        var (renderX, renderY) = ToTargetPoint(point.X, point.Y, renderProfile);
        return Math.Sqrt(renderX * renderX + renderY * renderY);
    }

    /// <summary>
    /// 计算两个 Kpc 点在默认渲染坐标系中的欧氏距离。
    /// </summary>
    /// <param name="left">第一个 Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <param name="right">第二个 Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <returns>两点映射到默认渲染坐标系后的欧氏距离。</returns>
    /// <seealso cref="CoordinateProfile.DefaultRenderProfile"/>
    internal static double GetKpcScreenDistance(
        (double X, double Y) left,
        (double X, double Y) right
    ) => GetKpcScreenDistance(left, right, RenderProfileDefault);

    /// <summary>
    /// 计算两个 Kpc 点在指定渲染坐标系中的欧氏距离。
    /// </summary>
    /// <param name="left">第一个 Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <param name="right">第二个 Kpc 点坐标 <c>(X, Y)</c>。</param>
    /// <param name="renderProfile">渲染坐标配置，用于将 Kpc 坐标差值映射到渲染空间。</param>
    /// <returns>两点映射到 <paramref name="renderProfile"/> 坐标系后的欧氏距离。</returns>
    /// <remarks>
    /// 距离计算通过对坐标差值（增量）进行比例缩放实现，不做原点平移，
    /// 保证在非对称坐标系下仍能正确反映屏幕空间的几何距离。
    /// </remarks>
    internal static double GetKpcScreenDistance(
        (double X, double Y) left,
        (double X, double Y) right,
        in CoordinateProfile renderProfile
    )
    {
        var deltaRenderX = ToTargetDeltaXCore(left.X - right.X, renderProfile);
        var deltaRenderY = ToTargetDeltaYCore(left.Y - right.Y, renderProfile);
        return Math.Sqrt(deltaRenderX * deltaRenderX + deltaRenderY * deltaRenderY);
    }
}
