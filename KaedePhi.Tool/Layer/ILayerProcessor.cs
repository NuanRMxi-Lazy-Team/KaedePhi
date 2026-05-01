using EventLayer = KaedePhi.Core.KaedePhi.EventLayer;
using KaedePhi.Tool.Common;

namespace KaedePhi.Tool.Layer;

/// <summary>
/// 谱面事件层级处理器：提供多层级的合并、切割与压缩功能。
/// </summary>
public interface ILayerProcessor<TLayer> : ILoggable
{
    /// <summary>
    /// 将多个事件层合并为单层（固定采样）。
    /// 有重叠区间时按等长切片逐段相加。
    /// </summary>
    /// <param name="layers">待合并的事件层列表。</param>
    /// <param name="precision">每拍内的采样步数；越大精度越高，计算量越大。</param>
    /// <returns>合并后的单个事件层。</returns>
    TLayer LayerMerge(List<TLayer> layers, double precision);

    /// <summary>
    /// 将多个事件层合并为单层（自适应采样）。
    /// 以事件边界为强制切割点，仅在误差超过容差时插入新采样段，天然压缩。
    /// </summary>
    /// <param name="layers">待合并的事件层列表。</param>
    /// <param name="precision">自适应采样的最大步数上限（同时作为事件合并精度）。</param>
    /// <param name="tolerance">误差容差百分比，决定何时插入额外切割点。</param>
    /// <returns>合并后的单个事件层。</returns>
    TLayer LayerMergePlus(List<TLayer> layers, double precision, double tolerance);

    /// <summary>
    /// 将单个事件层中各通道事件按指定精度切割为等长段。
    /// </summary>
    /// <param name="layer">待切割的事件层。</param>
    /// <param name="precision">每拍内的切割步数。</param>
    /// <returns>切割后的事件层。</returns>
    TLayer CutLayerEvents(TLayer layer, double precision);

    /// <summary>
    /// 将多个事件层中各通道事件按指定精度切割为等长段。
    /// </summary>
    /// <param name="layers">待切割的事件层列表。</param>
    /// <param name="precision">每拍内的切割步数。</param>
    /// <returns>切割后的事件层列表。</returns>
    List<TLayer> CutLayerEvents(List<TLayer> layers, double precision);

    /// <summary>
    /// 压缩事件层中各通道的事件列表，合并变化率相近的相邻线性事件。
    /// </summary>
    /// <param name="layer">待压缩的事件层（原地修改）。</param>
    /// <param name="tolerance">容差百分比，越大拟合精细度越低。</param>
    void LayerEventsCompress(TLayer layer, double tolerance);
}
