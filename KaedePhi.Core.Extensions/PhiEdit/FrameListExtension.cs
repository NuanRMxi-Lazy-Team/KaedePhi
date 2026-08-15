using System;
using System.Collections.Generic;
using KaedePhi.Core.PhiEdit;

namespace KaedePhi.Core.Extensions.PhiEdit
{
    public static class FrameListExtension
    {
        /// <summary>
        /// 将帧插入到列表
        /// 使用前必须确保列表已经按照Beat升序排序
        /// </summary>
        /// <param name="frameList">帧列表</param>
        /// <param name="frame">帧</param>
        /// <exception cref="ArgumentNullException">帧列表为 <see langword="null"/></exception>
        public static void AppendFrame(this List<Frame> frameList, Frame frame)
        {
            if (frameList is null)
                throw new ArgumentNullException(nameof(frameList));
            var index = frameList.FindIndex(f => f.Beat > frame.Beat);
            if (index == -1)
                frameList.Add(frame);
            else
                frameList.Insert(index, frame);
        }

        /// <summary>
        /// 将移动帧插入到列表
        /// 使用前必须确保列表已经按照Beat升序排序
        /// </summary>
        /// <param name="frameList">移动帧列表</param>
        /// <param name="frame">移动帧</param>
        /// <exception cref="ArgumentNullException">帧列表为 <see langword="null"/></exception>
        public static void AppendMoveFrame(this List<MoveFrame> frameList, MoveFrame frame)
        {
            if (frameList is null)
                throw new ArgumentNullException(nameof(frameList));
            var index = frameList.FindIndex(f => f.Beat > frame.Beat);
            if (index == -1)
                frameList.Add(frame);
            else
                frameList.Insert(index, frame);
        }
    }
}
