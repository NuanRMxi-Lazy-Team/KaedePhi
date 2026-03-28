using PhiFanmade.Tool.PhiEdit;

namespace PhiFanmade.Tool.PhiEdit;

/// <summary>
/// 兼容旧 API 的包装器。新代码请改用 <see cref="PeCoordinateTransform"/>。
/// </summary>
[Obsolete("Use PhiFanmade.Tool.PhiEdit.PeCoordinateTransform instead.")]
public static class PhiEditHelper
{
    public static class CoordinateTransform
    {
        public static float ToRePhiEditX(float x) => PeCoordinateTransform.ToRePhiEditX(x);
        public static float ToRePhiEditY(float y) => PeCoordinateTransform.ToRePhiEditY(y);
    }
}
