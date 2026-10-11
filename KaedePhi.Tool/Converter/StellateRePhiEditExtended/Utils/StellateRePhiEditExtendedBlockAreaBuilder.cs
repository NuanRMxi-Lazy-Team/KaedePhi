using KaedePhi.Core.Primitives;
using KaedePhi.Tool.Common;
using KaedePhi.Tool.Converter.RePhiEdit.Model;
using KaedePhi.Tool.Converter.RePhiEdit.Utils;
using RpeJudgeLine = KaedePhi.Core.Formats.RePhiEdit.Model.JudgeLine;
using RpeEventLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.EventLayer;
using RpeExtendLayer = KaedePhi.Core.Formats.RePhiEdit.Model.Events.ExtendLayer;
using IrBlockArea = KaedePhi.Core.Intermediate.Model.BlockArea;
using IrEvent = KaedePhi.Core.Intermediate.Model.Events.Event<double>;
using RpeEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<float>;
using RpeAlphaEvent = KaedePhi.Core.Formats.RePhiEdit.Model.Events.Event<int>;
using RpeTransform = KaedePhi.Tool.Converter.RePhiEdit.Utils.Transform;

namespace KaedePhi.Tool.Converter.StellateRePhiEditExtended.Utils;

/// <summary>
/// 将 IR 噪域构建为 StellateRePhiEditExtended 标记判定线。
/// </summary>
public static class StellateRePhiEditExtendedBlockAreaBuilder
{
    private const double MarkerTextureWidth = 900d;
    private const double MarkerTextureHeight = 900d;
    private const double RenderWidth = 1350d;
    private const double RenderHeight = 900d;
    private const double IrCoordinateSpan = 2d;

    /// <summary>
    /// 将一个 IR 噪域转换为 RePhiEdit 标记判定线。
    /// </summary>
    /// <param name="source">待转换的 IR 噪域</param>
    /// <param name="index">噪域在谱面中的索引</param>
    /// <param name="options">事件切分选项</param>
    /// <param name="warnLogger">接收转换警告的回调</param>
    /// <param name="cancellationToken">用于取消转换的令牌</param>
    /// <returns>表示该噪域的 RePhiEdit 判定线</returns>
    public static RpeJudgeLine ConvertBlockArea(
        IrBlockArea source,
        int index,
        ConvertOption.CuttingOptions options,
        Action<string>? warnLogger,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCoordinates(source);
        ValidateLifecycle(source, index);

        var centerX = source.TopRightX / 2d + source.BottomLeftX / 2d;
        var centerY = source.TopRightY / 2d + source.BottomLeftY / 2d;
        var width = Math.Abs(source.TopRightX - source.BottomLeftX);
        var height = Math.Abs(source.TopRightY - source.BottomLeftY);
        if (!double.IsFinite(centerX) || !double.IsFinite(centerY))
            throw new FormatException($"IR 噪域 {index} 的中心坐标无法转换。");

        var baseScaleX = StellateBlockAreaEventConverter.ToRpeFloat(
            width * RenderWidth / (IrCoordinateSpan * MarkerTextureWidth),
            index,
            "宽度"
        );
        var baseScaleY = StellateBlockAreaEventConverter.ToRpeFloat(
            height * RenderHeight / (IrCoordinateSpan * MarkerTextureHeight),
            index,
            "高度"
        );

        var hasAnchorEvents =
            StellateBlockAreaEventConverter.HasEvents(source.RotateAnchorXEvents)
            || StellateBlockAreaEventConverter.HasEvents(source.RotateAnchorYEvents)
            || StellateBlockAreaEventConverter.HasEvents(source.ScaleAnchorXEvents)
            || StellateBlockAreaEventConverter.HasEvents(source.ScaleAnchorYEvents);
        var moveXEvents = source.MoveXEvents;
        var moveYEvents = source.MoveYEvents;
        var moveDefaultX = centerX;
        var moveDefaultY = centerY;
        if (hasAnchorEvents)
        {
            var bakedMotion = StellateBlockAreaAnchorMotionBaker.BakeAnchorMotion(
                source,
                index,
                options.UnsupportedEasingPrecision,
                cancellationToken
            );
            moveXEvents = bakedMotion.MoveXEvents;
            moveYEvents = bakedMotion.MoveYEvents;
            moveDefaultX = bakedMotion.CenterX;
            moveDefaultY = bakedMotion.CenterY;
            warnLogger?.Invoke(
                $"IR 噪域 {index} 的锚点变换已按每拍 {options.UnsupportedEasingPrecision} 段采样并烘焙到位置事件；锚点事件本身不会保留。"
            );
        }

        var eventLayer = new RpeEventLayer
        {
            MoveXEvents = StellateBlockAreaEventConverter.ConvertEvents(
                moveXEvents,
                moveDefaultX,
                RpeTransform.TransformToRpeX,
                options,
                index,
                "X 轴移动",
                ensureBaseline: true,
                cancellationToken
            ),
            MoveYEvents = StellateBlockAreaEventConverter.ConvertEvents(
                moveYEvents,
                moveDefaultY,
                RpeTransform.TransformToRpeY,
                options,
                index,
                "Y 轴移动",
                ensureBaseline: true,
                cancellationToken
            ),
            RotateEvents = StellateBlockAreaEventConverter.ConvertEvents(
                source.RotateEvents,
                0d,
                RpeTransform.TransformToRpeAngle,
                options,
                index,
                "旋转",
                ensureBaseline: true,
                cancellationToken
            ),
            AlphaEvents = StellateBlockAreaEventConverter.CreateAlphaEvents(source),
            SpeedEvents =
            [
                StellateBlockAreaEventConverter.CreateMarker(source.AppearBeat, 1),
                StellateBlockAreaEventConverter.CreateMarker(source.EnableBeat, 2),
                StellateBlockAreaEventConverter.CreateMarker(source.DisableBeat, 3),
                StellateBlockAreaEventConverter.CreateMarker(source.DisappearBeat, 4),
            ],
        };

        return new RpeJudgeLine
        {
            Name = $"BlockArea_{index + 1}",
            Texture = source.IsSubtract
                ? @"Pictures\isSubtract1.png"
                : @"Pictures\isSubtract0.png",
            Anchor = [0.5f, 0.5f],
            Father = -1,
            RotateWithFather = false,
            EventLayers = [eventLayer],
            Extended = new RpeExtendLayer
            {
                ScaleXEvents = StellateBlockAreaEventConverter.ConvertEvents(
                    source.ScaleXEvents,
                    1d,
                    value => baseScaleX * value,
                    options,
                    index,
                    "X 轴缩放",
                    ensureBaseline: true,
                    cancellationToken
                ),
                ScaleYEvents = StellateBlockAreaEventConverter.ConvertEvents(
                    source.ScaleYEvents,
                    1d,
                    value => baseScaleY * value,
                    options,
                    index,
                    "Y 轴缩放",
                    ensureBaseline: true,
                    cancellationToken
                ),
            },
        };
    }

    private static void ValidateCoordinates(IrBlockArea source)
    {
        if (
            !double.IsFinite(source.TopRightX)
            || !double.IsFinite(source.TopRightY)
            || !double.IsFinite(source.BottomLeftX)
            || !double.IsFinite(source.BottomLeftY)
        )
            throw new FormatException("IR 噪域坐标必须是有限数值。");

        if (
            !double.IsFinite(Math.Abs(source.TopRightX - source.BottomLeftX))
            || !double.IsFinite(Math.Abs(source.TopRightY - source.BottomLeftY))
        )
            throw new FormatException("IR 噪域尺寸超出 RePhiEdit 可编码范围。");
    }

    private static void ValidateLifecycle(IrBlockArea source, int index)
    {
        if (
            source.AppearBeat > source.EnableBeat
            || source.EnableBeat > source.DisableBeat
            || source.DisableBeat > source.DisappearBeat
        )
            throw new FormatException(
                $"IR 噪域 {index} 的显示阶段顺序无效，无法转换为 StellateRePhiEditExtended。"
            );
    }

}
