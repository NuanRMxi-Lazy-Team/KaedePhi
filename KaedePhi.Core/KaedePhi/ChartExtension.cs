namespace KaedePhi.Core.KaedePhi
{
    public partial class Chart
    {
        public Chart Clone()
        {
            return new Chart
            {
                BpmList = BpmList.ConvertAll(bpm => bpm.Clone()),
                Meta = Meta.Clone(),
                // 跳过空判定线，将空集合视为正常谱面
                JudgeLineList = JudgeLineList
                    .FindAll(judgeLine => judgeLine is not null)
                    .ConvertAll(judgeLine => judgeLine.Clone()),
            };
        }
    }
}
