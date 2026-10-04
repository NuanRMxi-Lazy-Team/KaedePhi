using System.Collections.Generic;
using KaedePhi.Core.Primitives;

namespace KaedePhi.Core.Intermediate.Model
{
    /// <summary>
    /// 表示使用区间事件驱动的噪域。
    /// </summary>
    public class BlockArea
    {
        /// <summary>
        /// 获取或设置区域右上对角点的 X 坐标。
        /// </summary>
        public double TopRightX { get; set; }

        /// <summary>
        /// 获取或设置区域右上对角点的 Y 坐标。
        /// </summary>
        public double TopRightY { get; set; }

        /// <summary>
        /// 获取或设置区域左下对角点的 X 坐标。
        /// </summary>
        public double BottomLeftX { get; set; }

        /// <summary>
        /// 获取或设置区域左下对角点的 Y 坐标。
        /// </summary>
        public double BottomLeftY { get; set; }

        /// <summary>
        /// 获取或设置区域开始显示的拍。
        /// </summary>
        public Beat AppearBeat { get; set; }

        /// <summary>
        /// 获取或设置区域开始激活的拍。
        /// </summary>
        public Beat EnableBeat { get; set; }

        /// <summary>
        /// 获取或设置区域结束激活的拍。
        /// </summary>
        public Beat DisableBeat { get; set; }

        /// <summary>
        /// 获取或设置区域停止显示的拍。
        /// </summary>
        public Beat DisappearBeat { get; set; }

        /// <summary>
        /// 获取或设置区域是否为减算区域。
        /// </summary>
        public bool IsSubtract { get; set; }

        /// <summary>
        /// 获取或设置区域中心 X 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? MoveXEvents { get; set; }

        /// <summary>
        /// 获取或设置区域中心 Y 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? MoveYEvents { get; set; }

        /// <summary>
        /// 获取或设置区域旋转角度的区间事件。
        /// </summary>
        public List<Events.Event<double>>? RotateEvents { get; set; }

        /// <summary>
        /// 获取或设置区域旋转中心 X 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? RotateAnchorXEvents { get; set; }

        /// <summary>
        /// 获取或设置区域旋转中心 Y 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? RotateAnchorYEvents { get; set; }

        /// <summary>
        /// 获取或设置区域缩放 X 倍率的区间事件。
        /// </summary>
        public List<Events.Event<double>>? ScaleXEvents { get; set; }

        /// <summary>
        /// 获取或设置区域缩放 Y 倍率的区间事件。
        /// </summary>
        public List<Events.Event<double>>? ScaleYEvents { get; set; }

        /// <summary>
        /// 获取或设置区域缩放中心 X 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? ScaleAnchorXEvents { get; set; }

        /// <summary>
        /// 获取或设置区域缩放中心 Y 坐标的区间事件。
        /// </summary>
        public List<Events.Event<double>>? ScaleAnchorYEvents { get; set; }

        /// <summary>
        /// 深拷贝噪域。
        /// </summary>
        /// <returns>噪域副本。</returns>
        public BlockArea Clone() =>
            new()
            {
                TopRightX = TopRightX,
                TopRightY = TopRightY,
                BottomLeftX = BottomLeftX,
                BottomLeftY = BottomLeftY,
                AppearBeat = new Beat((int[])AppearBeat),
                EnableBeat = new Beat((int[])EnableBeat),
                DisableBeat = new Beat((int[])DisableBeat),
                DisappearBeat = new Beat((int[])DisappearBeat),
                IsSubtract = IsSubtract,
                MoveXEvents = CloneEvents(MoveXEvents),
                MoveYEvents = CloneEvents(MoveYEvents),
                RotateEvents = CloneEvents(RotateEvents),
                RotateAnchorXEvents = CloneEvents(RotateAnchorXEvents),
                RotateAnchorYEvents = CloneEvents(RotateAnchorYEvents),
                ScaleXEvents = CloneEvents(ScaleXEvents),
                ScaleYEvents = CloneEvents(ScaleYEvents),
                ScaleAnchorXEvents = CloneEvents(ScaleAnchorXEvents),
                ScaleAnchorYEvents = CloneEvents(ScaleAnchorYEvents),
            };

        private static List<Events.Event<T>>? CloneEvents<T>(List<Events.Event<T>>? events)
            where T : notnull => events?.ConvertAll(evt => evt.Clone());
    }
}
