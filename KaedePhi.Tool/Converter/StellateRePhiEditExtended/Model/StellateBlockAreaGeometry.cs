namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Model;

/// <summary>
/// 表示噪域在指定时刻的中心、尺寸和旋转角度。
/// </summary>
/// <param name="CenterX">中心横坐标</param>
/// <param name="CenterY">中心纵坐标</param>
/// <param name="Width">宽度</param>
/// <param name="Height">高度</param>
/// <param name="Rotation">旋转角度</param>
public readonly record struct StellateBlockAreaGeometry(
    double CenterX,
    double CenterY,
    double Width,
    double Height,
    double Rotation
);
