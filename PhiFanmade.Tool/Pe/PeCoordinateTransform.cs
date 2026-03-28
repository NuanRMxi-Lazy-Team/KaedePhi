namespace PhiFanmade.Tool.PhiEdit;

/// <summary>
/// PhiEdit 与 RePhiEdit 之间的坐标换算工具。
/// </summary>
public static class PeCoordinateTransform
{
    public static float ToRePhiEditX(float x)
    {
        var rpeMin = Rpe.Chart.CoordinateSystem.MinX;
        var rpeMax = Rpe.Chart.CoordinateSystem.MaxX;
        var peMin = Pe.Chart.CoordinateSystem.MinX;
        var peMax = Pe.Chart.CoordinateSystem.MaxX;
        return rpeMin + (x - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
    }

    public static float ToRePhiEditY(float y)
    {
        var rpeMin = Rpe.Chart.CoordinateSystem.MinY;
        var rpeMax = Rpe.Chart.CoordinateSystem.MaxY;
        var peMin = Pe.Chart.CoordinateSystem.MinY;
        var peMax = Pe.Chart.CoordinateSystem.MaxY;
        return rpeMin + (y - peMin) / (peMax - peMin) * (rpeMax - rpeMin);
    }
}

