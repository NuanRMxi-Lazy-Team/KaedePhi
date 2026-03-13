using PhiFanmade.Core.RePhiEdit;
using PhiFanmade.Tool.RePhiEdit.Internal;

namespace PhiFanmade.Tool.RePhiEdit;

/// <summary>
/// 所有外部暴露的方法都在这里
/// </summary>
public static class RePhiEditHelper
{
    public static Action<string> OnInfo = s => { };
    public static Action<string> OnWarning = s => { };
    public static Action<string> OnError = s => { };
    public static Action<string> OnDebug = s => { };

    /// <summary>
    /// 在有父线的情况下，获得一条判定线的绝对位置
    /// </summary>
    /// <param name="fatherLineX">父线X轴坐标</param>
    /// <param name="fatherLineY">父线Y轴坐标</param>
    /// <param name="angleDegrees">父线旋转角度</param>
    /// <param name="lineX">当前线相对于父线的X轴坐标</param>
    /// <param name="lineY">当前线相对于父线的Y轴坐标</param>
    /// <returns>当前线绝对坐标</returns>
    public static (double, double) GetLinePos(double fatherLineX, double fatherLineY, double angleDegrees,
        double lineX, double lineY)
        => FatherUnbindProcessor.GetLinePos(fatherLineX, fatherLineY, angleDegrees, lineX, lineY);

    #region classic

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">精度，数值越大，切得越细</param>
    /// <param name="tolerance">容差百分比</param>
    /// <param name="compress">是否压缩事件列表（根据容差合并变化值相近的多个线性事件）</param>
    /// <returns>经过解绑的判定线</returns>
    public static JudgeLine FatherUnbind(int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d, bool compress = true)
        => FatherUnbindProcessor.FatherUnbind(targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindProcessor.ChartCacheTable.GetOrCreateValue(allJudgeLines), compress);

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此方法不会将原有的所有层级合并。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">精度，数值越大，切得越细</param>
    /// <param name="tolerance">容差百分比</param>
    /// <param name="compress">是否压缩事件列表（根据容差合并变化值相近的多个线性事件）</param>
    /// <returns>经过解绑的判定线</returns>
    public static async Task<JudgeLine> FatherUnbindAsync(int targetJudgeLineIndex,
        List<JudgeLine> allJudgeLines, double precision = 64d, double tolerance = 5d, bool compress = true)
        => await FatherUnbindAsyncProcessor.FatherUnbindAsync(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance, FatherUnbindProcessor.ChartCacheTable.GetOrCreateValue(allJudgeLines), compress);

    /// <summary>
    /// 根据容差，压缩事件列表
    /// </summary>
    /// <param name="events"></param>
    /// <param name="tolerance"></param>
    /// <returns>压缩后的事件列表</returns>
    public static List<Event<float>> EventListCompress(List<Event<float>> events, double tolerance = 5)
        => EventProcessor.EventListCompress(events, tolerance);


    /// <summary>
    /// 将两个事件列表合并，如果有重合事件则发出警告
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <param name="precision">切割精细度</param>
    /// <param name="tolerance">容差百分比</param>
    /// <param name="compress">是否使用容差百分比进行压缩</param>
    /// <returns>已合并的事件列表</returns>
    public static List<Event<T>> EventListMerge<T>(
        List<Event<T>> toEvents, List<Event<T>> formEvents, double precision = 64d,
        double tolerance = 5d, bool compress = true)
        => EventProcessor.EventListMerge(toEvents, formEvents, precision, tolerance, compress);

    /// <summary>
    /// 层级合并方法，会将传入层级所有可合并层级的数值全部合并到第一层级中，注意，此方法会破坏工程可编辑性。
    /// </summary>
    /// <param name="layers">多个层级</param>
    /// <param name="precision">切割精细度，越大精度越高</param>
    /// <param name="tolerance">容差百分比</param>
    /// <param name="compress">是否使用容差百分比进行压缩</param>
    /// <returns>已合并的事件列表</returns>
    public static EventLayer LayerMerge(List<EventLayer> layers, double precision = 64d, double tolerance = 5d,
        bool compress = true)
        => LayerProcessor.LayerMerge(layers, precision, tolerance, compress);

    /// <summary>
    /// 将事件层级上的所有事件全部切割到指定精度的时间点上，注意，此方法会破坏工程可编辑性。
    /// </summary>
    /// <param name="layer">单个层级</param>
    /// <param name="precision">精度，越大越精细</param>
    /// <param name="tolerance">容差，仅在压缩启用时有效</param>
    /// <param name="compress">是否压缩（默认压缩）</param>
    public static EventLayer CutLayerEvents(EventLayer layer, double precision = 64d, double tolerance = 5d,
        bool compress = true)
        => LayerProcessor.CutLayerEvents(layer, precision, tolerance, compress);

    /// <summary>
    /// 将事件层级上的所有事件全部切割到指定精度的时间点上，注意，此方法会破坏工程可编辑性。
    /// </summary>
    /// <param name="layers">多个层级</param>
    /// <param name="precision">精度，越大越精细</param>
    /// <param name="tolerance">容差，仅在压缩启用时有效</param>
    /// <param name="compress">是否压缩（默认压缩）</param>
    public static List<EventLayer> CutLayerEvents(List<EventLayer> layers, double precision = 64d,
        double tolerance = 5d,
        bool compress = true)
        => LayerProcessor.CutLayerEvents(layers, precision, tolerance, compress);

    #endregion

    #region plus

    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此函数不会将原有的所有层级合并。
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">精度，数值越大，切得越细</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>经过解绑的判定线</returns>
    public static JudgeLine FatherUnbindPlus(int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => FatherUnbindProcessor.FatherUnbindPlus(targetJudgeLineIndex, allJudgeLines, precision, tolerance,
            FatherUnbindProcessor.ChartCacheTable.GetOrCreateValue(allJudgeLines));


    /// <summary>
    /// 将判定线与自己的父判定线解绑，并保持行为一致，注意，此方法不会将原有的所有层级合并。(异步多线程版本)
    /// </summary>
    /// <param name="targetJudgeLineIndex">需要解绑的判定线索引</param>
    /// <param name="allJudgeLines">所有判定线</param>
    /// <param name="precision">精度，数值越大，切得越细</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>经过解绑的判定线</returns>
    public static async Task<JudgeLine> FatherUnbindPlusAsync(int targetJudgeLineIndex, List<JudgeLine> allJudgeLines,
        double precision = 64d, double tolerance = 5d)
        => await FatherUnbindAsyncProcessor.FatherUnbindPlusAsync(targetJudgeLineIndex, allJudgeLines,
            precision, tolerance, FatherUnbindProcessor.ChartCacheTable.GetOrCreateValue(allJudgeLines));


    /// <summary>
    /// 更简易的事件列表合并方法，也许更省性能，不支持控制是否压缩
    /// </summary>
    /// <param name="toEvents">源事件列表</param>
    /// <param name="formEvents">要合并进源事件的事件列表</param>
    /// <param name="precision">切割精细度</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>已合并的事件列表</returns>
    public static List<Event<T>> EventMergePlus<T>(List<Event<T>> toEvents, List<Event<T>> formEvents,
        double precision = 64d, double tolerance = 5d)
        => EventProcessor.EventMergePlus(toEvents, formEvents, precision, tolerance);


    /// <summary>
    /// 更节省性能的层级合并方法，会将传入层级所有可合并层级的数值全部合并到第一层级中，注意，此方法会破坏工程可编辑性。
    /// </summary>
    /// <param name="layers">多个层级</param>
    /// <param name="precision">切割精细度，越大精度越高</param>
    /// <param name="tolerance">容差百分比</param>
    /// <returns>已合并的事件列表</returns>
    public static EventLayer LayerMergePlus(List<EventLayer> layers, double precision = 64d, double tolerance = 5d)
        => LayerProcessor.LayerMergePlus(layers, precision, tolerance);

    #endregion
}