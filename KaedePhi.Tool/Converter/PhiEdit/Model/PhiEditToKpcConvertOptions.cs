namespace KaedePhi.Tool.Converter.PhiEdit.Model;

public class PhiEditToKpcConvertOptions
{
    /// <summary>
    /// PhiEdit帧转事件后持续拍长度
    /// </summary>
    public double FrameDurationBeat { get; set; } = 1 / 64d;
}