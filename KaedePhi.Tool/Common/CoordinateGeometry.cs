namespace KaedePhi.Tool.Common;

/// <summary>
/// 坐标几何换算工具。
/// Kpc 作为统一归一化坐标空间；坐标转换和距离评估在可配置的渲染空间进行。
/// 判定线偏移旋转在物理等比空间执行，以匹配 RPE 引擎（prpr）的父子坐标变换语义：
/// 旋转前将 Kpc Y 折算到以半宽为单位的物理等比坐标（×halfH/halfW），旋转后还原（÷halfH/halfW）。
/// </summary>
internal static class CoordinateGeometry
{
    private static readonly CoordinateProfile KpcProfile = CoordinateProfile.KpcProfile;
    private static readonly CoordinateProfile RenderProfileDefault = CoordinateProfile.DefaultRenderProfile;

    /// <summary>
    /// 计算坐标轴跨度，并校验跨度不为零。
    /// </summary>
    /// <param name="min">坐标轴最小值。</param>
    /// <param name="max">坐标轴最大值。</param>
    /// <param name="axisName">轴名称，仅用于异常信息。</param>
    /// <returns>坐标轴跨度。</returns>
    /// <exception cref="InvalidOperationException">当坐标轴跨度接近零时抛出。</exception>
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
    /// <param name="axisName">轴名称，仅用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标值。</returns>
    private static double MapValue(double value, double sourceMin, double sourceMax, double targetMin, double targetMax,
        string axisName)
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return targetMin + (value - sourceMin) / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将坐标增量从源区间按比例映射到目标区间。
    /// </summary>
    /// <param name="delta">待映射的源坐标增量。</param>
    /// <param name="sourceMin">源区间最小值。</param>
    /// <param name="sourceMax">源区间最大值。</param>
    /// <param name="targetMin">目标区间最小值。</param>
    /// <param name="targetMax">目标区间最大值。</param>
    /// <param name="axisName">轴名称，仅用于跨度校验错误定位。</param>
    /// <returns>映射后的目标坐标增量。</returns>
    private static double MapDelta(double delta, double sourceMin, double sourceMax, double targetMin, double targetMax,
        string axisName)
    {
        var sourceSpan = GetSpan(sourceMin, sourceMax, axisName);
        var targetSpan = GetSpan(targetMin, targetMax, axisName);
        return delta / sourceSpan * targetSpan;
    }

    /// <summary>
    /// 将 Kpc X 坐标映射到目标坐标系。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    private static double ToTargetXCore(double x, CoordinateProfile target)
        => MapValue(x, KpcProfile.MinX, KpcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 Kpc Y 坐标映射到目标坐标系。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    private static double ToTargetYCore(double y, CoordinateProfile target)
        => MapValue(y, KpcProfile.MinY, KpcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将源坐标系 X 坐标映射到 Kpc。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 坐标系下的 X 坐标。</returns>
    private static double ToKpcXCore(double x, CoordinateProfile source)
        => MapValue(x, source.MinX, source.MaxX, KpcProfile.MinX, KpcProfile.MaxX, "X");

    /// <summary>
    /// 将源坐标系 Y 坐标映射到 Kpc。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 坐标系下的 Y 坐标。</returns>
    private static double ToKpcYCore(double y, CoordinateProfile source)
        => MapValue(y, source.MinY, source.MaxY, KpcProfile.MinY, KpcProfile.MaxY, "Y");

    /// <summary>
    /// 将 Kpc X 增量映射到目标坐标系增量。
    /// </summary>
    /// <param name="x">Kpc X 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 增量。</returns>
    private static double ToTargetDeltaXCore(double x, CoordinateProfile target)
        => MapDelta(x, KpcProfile.MinX, KpcProfile.MaxX, target.MinX, target.MaxX, "X");

    /// <summary>
    /// 将 Kpc Y 增量映射到目标坐标系增量。
    /// </summary>
    /// <param name="y">Kpc Y 增量。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 增量。</returns>
    private static double ToTargetDeltaYCore(double y, CoordinateProfile target)
        => MapDelta(y, KpcProfile.MinY, KpcProfile.MaxY, target.MinY, target.MaxY, "Y");

    /// <summary>
    /// 将 Kpc 角度转换到目标坐标系角度方向。
    /// </summary>
    /// <param name="KpcAngleDegrees">Kpc 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的角度（度）。</returns>
    private static double ToTargetAngleCore(double KpcAngleDegrees, CoordinateProfile target)
        => target.ClockwiseRotation == KpcProfile.ClockwiseRotation ? KpcAngleDegrees : -KpcAngleDegrees;

    /// <summary>
    /// 将源坐标系角度转换到 Kpc 角度方向。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 角度（度）。</returns>
    private static double ToKpcAngleCore(double sourceAngleDegrees, CoordinateProfile source)
        => source.ClockwiseRotation == KpcProfile.ClockwiseRotation ? sourceAngleDegrees : -sourceAngleDegrees;

    /// <summary>
    /// 将 Kpc X 坐标转换为指定坐标系 X 坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 X 坐标。</returns>
    internal static double ToTargetX(double x, in CoordinateProfile target) => ToTargetXCore(x, target);

    /// <summary>
    /// 将 Kpc Y 坐标转换为指定坐标系 Y 坐标。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的 Y 坐标。</returns>
    internal static double ToTargetY(double y, in CoordinateProfile target) => ToTargetYCore(y, target);

    /// <summary>
    /// 将 Kpc X 坐标转换为指定坐标系 X 坐标（单精度）。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的单精度 X 坐标。</returns>
    internal static float ToTargetXf(double x, in CoordinateProfile target) => (float)ToTargetXCore(x, target);

    /// <summary>
    /// 将 Kpc Y 坐标转换为指定坐标系 Y 坐标（单精度）。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系下的单精度 Y 坐标。</returns>
    internal static float ToTargetYf(double y, in CoordinateProfile target) => (float)ToTargetYCore(y, target);

    /// <summary>
    /// 将指定坐标系 X 坐标转换为 Kpc X 坐标。
    /// </summary>
    /// <param name="x">源坐标系 X 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc X 坐标。</returns>
    internal static double ToKpcX(double x, in CoordinateProfile source) => ToKpcXCore(x, source);

    /// <summary>
    /// 将默认渲染坐标系 X 坐标转换为 Kpc X 坐标。
    /// </summary>
    /// <param name="x">默认渲染坐标系 X 坐标。</param>
    /// <returns>Kpc X 坐标。</returns>
    internal static double ToKpcX(double x) => ToKpcXCore(x, RenderProfileDefault);

    /// <summary>
    /// 将指定坐标系 Y 坐标转换为 Kpc Y 坐标。
    /// </summary>
    /// <param name="y">源坐标系 Y 坐标。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc Y 坐标。</returns>
    internal static double ToKpcY(double y, in CoordinateProfile source) => ToKpcYCore(y, source);

    /// <summary>
    /// 将默认渲染坐标系 Y 坐标转换为 Kpc Y 坐标。
    /// </summary>
    /// <param name="y">默认渲染坐标系 Y 坐标。</param>
    /// <returns>Kpc Y 坐标。</returns>
    internal static double ToKpcY(double y) => ToKpcYCore(y, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc 角度转换为指定坐标系角度。
    /// </summary>
    /// <param name="KpcAngleDegrees">Kpc 角度（度）。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系角度（度）。</returns>
    internal static double ToTargetAngle(double KpcAngleDegrees, in CoordinateProfile target)
        => ToTargetAngleCore(KpcAngleDegrees, target);

    /// <summary>
    /// 将指定坐标系角度转换为 Kpc 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">源坐标系角度（度）。</param>
    /// <param name="source">源坐标配置。</param>
    /// <returns>Kpc 角度（度）。</returns>
    internal static double ToKpcAngle(double sourceAngleDegrees, in CoordinateProfile source)
        => ToKpcAngleCore(sourceAngleDegrees, source);

    /// <summary>
    /// 将默认渲染坐标系角度转换为 Kpc 角度。
    /// </summary>
    /// <param name="sourceAngleDegrees">默认渲染坐标系角度（度）。</param>
    /// <returns>Kpc 角度（度）。</returns>
    internal static double ToKpcAngle(double sourceAngleDegrees) =>
        ToKpcAngleCore(sourceAngleDegrees, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc X 坐标转换为默认渲染坐标系 X 坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <returns>默认渲染坐标系下的 X 坐标。</returns>
    internal static double ToRenderX(double x) => ToTargetXCore(x, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc Y 坐标转换为默认渲染坐标系 Y 坐标。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系下的 Y 坐标。</returns>
    internal static double ToRenderY(double y) => ToTargetYCore(y, RenderProfileDefault);

    /// <summary>
    /// 将 Kpc X 坐标转换为默认渲染坐标系 X 坐标（单精度）。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <returns>默认渲染坐标系下的单精度 X 坐标。</returns>
    internal static float ToRenderXf(double x) => (float)ToRenderX(x);

    /// <summary>
    /// 将 Kpc Y 坐标转换为默认渲染坐标系 Y 坐标（单精度）。
    /// </summary>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系下的单精度 Y 坐标。</returns>
    internal static float ToRenderYf(double y) => (float)ToRenderY(y);


    /// <summary>
    /// 将 Kpc 角度转换为默认渲染坐标系角度。
    /// </summary>
    /// <param name="KpcAngleDegrees">Kpc 角度（度）。</param>
    /// <returns>默认渲染坐标系角度（度）。</returns>
    internal static double ToRenderAngle(double KpcAngleDegrees) =>
        ToTargetAngleCore(KpcAngleDegrees, RenderProfileDefault);


    /// <summary>
    /// 将 Kpc 点坐标转换为默认渲染坐标系点坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <returns>默认渲染坐标系点坐标。</returns>
    internal static (double X, double Y) ToRenderPoint(double x, double y)
        => (ToRenderX(x), ToRenderY(y));

    /// <summary>
    /// 将 Kpc 点坐标转换为指定坐标系点坐标。
    /// </summary>
    /// <param name="x">Kpc X 坐标。</param>
    /// <param name="y">Kpc Y 坐标。</param>
    /// <param name="target">目标坐标配置。</param>
    /// <returns>目标坐标系点坐标。</returns>
    internal static (double X, double Y) ToTargetPoint(double x, double y, in CoordinateProfile target)
        => (ToTargetXCore(x, target), ToTargetYCore(y, target));

    /// <summary>
    /// 在物理等比空间中旋转 Kpc 偏移向量，旋转后还原到 Kpc 坐标。
    /// <para>
    /// RPE 引擎（prpr）的父子坐标变换在物理等比空间中执行：
    /// X 轴和 Y 轴都先除以半宽（halfW = renderProfile.SpanX / 2），再做旋转。
    /// 而 Kpc 坐标系中 X = rpe_x / halfW（正确），Y = rpe_y / halfH（halfH ≠ halfW），
    /// 因此旋转前需要将 Kpc Y 乘以 (halfH / halfW) 折算到物理等比空间，旋转后再除以该比例还原。
    /// 这等价于在旋转矩阵中对 Y 轴引入缩放因子 k = halfH / halfW（宽高比的倒数）。
    /// </para>
    /// <para>
    /// 屏幕空间的几何感知（如误差阈值、距离评估）由 <see cref="GetKpcScreenDistance"/>
    /// 和 <see cref="GetKpcScreenMagnitude"/> 负责，与旋转计算相互独立。
    /// </para>
    /// </summary>
    /// <param name="x">Kpc X 增量。</param>
    /// <param name="y">Kpc Y 增量。</param>
    /// <param name="angleDegrees">旋转角度（度，CCW 为正，与 Kpc 内部约定一致）。</param>
    /// <param name="renderProfile">渲染坐标配置，用于计算 X/Y 轴物理比例。</param>
    /// <returns>旋转后的 Kpc 增量向量。</returns>
    internal static (double X, double Y) RotateKpcOffset(
        double x, double y, double angleDegrees, in CoordinateProfile renderProfile)
    {
        // k = halfH / halfW = SpanY / SpanX
        // 物理等比空间：phys_x = Kpc_x，phys_y = Kpc_y * k
        // 旋转后还原：result_y = rotated_phys_y / k
        var spanX = GetSpan(renderProfile.MinX, renderProfile.MaxX, "X");
        var spanY = GetSpan(renderProfile.MinY, renderProfile.MaxY, "Y");
        var k = spanY / spanX; // halfH / halfW，典型值 900/1350 = 2/3
        var rad = angleDegrees * (Math.PI / 180d);
        var cos = Math.Cos(rad);
        var sin = Math.Sin(rad);
        // 等价于：先 y→ y*k，旋转，再 y→ y/k
        // rotX = x*cos - (y*k)*sin
        // rotY = (x*sin + (y*k)*cos) / k
        return (x * cos - y * k * sin,
                (x * sin + y * k * cos) / k);
    }

    /// <summary>
    /// 在物理等比空间中旋转 Kpc 偏移向量（使用默认渲染配置）。
    /// <para>
    /// 使用 <see cref="CoordinateProfile.DefaultRenderProfile"/> 作为宽高比参考。
    /// 详见带 <see cref="CoordinateProfile"/> 参数的重载。
    /// </para>
    /// </summary>
    /// <param name="x">Kpc X 增量。</param>
    /// <param name="y">Kpc Y 增量。</param>
    /// <param name="angleDegrees">旋转角度（度，CCW 为正）。</param>
    /// <returns>旋转后的 Kpc 增量向量。</returns>
    internal static (double X, double Y) RotateKpcOffset(double x, double y, double angleDegrees)
        => RotateKpcOffset(x, y, angleDegrees, RenderProfileDefault);

    /// <summary>
    /// 计算子线在 Kpc 下的绝对坐标。
    /// <para>
    /// 旋转在物理等比空间内进行（X 和 Y 都先折算到以半宽为单位的等比坐标），
    /// 以匹配 RPE 引擎（prpr）的父子坐标变换语义，保证解绑后子线位置在屏幕上与原谱面一致。
    /// </para>
    /// </summary>
    /// <param name="fatherLineX">父线 Kpc X 坐标。</param>
    /// <param name="fatherLineY">父线 Kpc Y 坐标。</param>
    /// <param name="angleDegrees">子线相对旋转角度（度，CCW 为正）。</param>
    /// <param name="lineX">子线相对 Kpc X 偏移。</param>
    /// <param name="lineY">子线相对 Kpc Y 偏移。</param>
    /// <returns>子线 Kpc 绝对坐标。</returns>
    internal static (double X, double Y) GetKpcAbsolutePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
    {
        var (rotX, rotY) = RotateKpcOffset(lineX, lineY, angleDegrees);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 计算子线在 Kpc 下的绝对坐标（指定渲染坐标配置）。
    /// <para>
    /// 旋转在物理等比空间内进行，<paramref name="renderProfile"/> 的宽高比用于确定物理等比折算系数，
    /// 以匹配 RPE 引擎（prpr）的父子坐标变换语义。
    /// </para>
    /// </summary>
    /// <param name="fatherLineX">父线 Kpc X 坐标。</param>
    /// <param name="fatherLineY">父线 Kpc Y 坐标。</param>
    /// <param name="angleDegrees">子线相对旋转角度（度，CCW 为正）。</param>
    /// <param name="lineX">子线相对 Kpc X 偏移。</param>
    /// <param name="lineY">子线相对 Kpc Y 偏移。</param>
    /// <param name="renderProfile">渲染坐标配置，用于物理等比折算。</param>
    /// <returns>子线 Kpc 绝对坐标。</returns>
    internal static (double X, double Y) GetKpcAbsolutePos(
        double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY, in CoordinateProfile renderProfile)
    {
        var (rotX, rotY) = RotateKpcOffset(lineX, lineY, angleDegrees, renderProfile);
        return (fatherLineX + rotX, fatherLineY + rotY);
    }

    /// <summary>
    /// 基于默认渲染坐标配置，计算 Kpc 点在屏幕空间中的模长。
    /// </summary>
    /// <param name="point">Kpc 点坐标。</param>
    /// <returns>屏幕空间模长。</returns>
    internal static double GetKpcScreenMagnitude((double X, double Y) point)
        => GetKpcScreenMagnitude(point, RenderProfileDefault);

    /// <summary>
    /// 基于指定渲染坐标配置，计算 Kpc 点在屏幕空间中的模长。
    /// </summary>
    /// <param name="point">Kpc 点坐标。</param>
    /// <param name="renderProfile">用于屏幕空间计算的渲染坐标配置。</param>
    /// <returns>屏幕空间模长。</returns>
    internal static double GetKpcScreenMagnitude((double X, double Y) point, in CoordinateProfile renderProfile)
    {
        var (renderX, renderY) = ToTargetPoint(point.X, point.Y, renderProfile);
        return Math.Sqrt(renderX * renderX + renderY * renderY);
    }

    /// <summary>
    /// 基于默认渲染坐标配置，计算两个 Kpc 点在屏幕空间中的距离。
    /// </summary>
    /// <param name="left">第一个 Kpc 点。</param>
    /// <param name="right">第二个 Kpc 点。</param>
    /// <returns>屏幕空间距离。</returns>
    internal static double GetKpcScreenDistance((double X, double Y) left, (double X, double Y) right)
        => GetKpcScreenDistance(left, right, RenderProfileDefault);

    /// <summary>
    /// 基于指定渲染坐标配置，计算两个 Kpc 点在屏幕空间中的距离。
    /// </summary>
    /// <param name="left">第一个 Kpc 点。</param>
    /// <param name="right">第二个 Kpc 点。</param>
    /// <param name="renderProfile">用于屏幕空间计算的渲染坐标配置。</param>
    /// <returns>屏幕空间距离。</returns>
    internal static double GetKpcScreenDistance(
        (double X, double Y) left,
        (double X, double Y) right,
        in CoordinateProfile renderProfile)
    {
        var deltaRenderX = ToTargetDeltaXCore(left.X - right.X, renderProfile);
        var deltaRenderY = ToTargetDeltaYCore(left.Y - right.Y, renderProfile);
        return Math.Sqrt(deltaRenderX * deltaRenderX + deltaRenderY * deltaRenderY);
    }
}