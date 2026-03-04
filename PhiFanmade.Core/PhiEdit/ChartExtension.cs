using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhiFanmade.Core.PhiEdit
{
    public partial class Chart
    {
        /// <summary>
        /// 从PhiEditChart格式的字符串加载谱面
        /// </summary>
        /// <param name="pec">PhiEditChart字符串</param>
        /// <returns>已反序列化谱面</returns>
        /// <exception cref="FormatException">格式不正确</exception>
        public static Chart Load(string pec)
        {
            var chart = new Chart();
            var judgeDict = new Dictionary<int, JudgeLine>();
            var lines = pec.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i == 0)
                {
                    if (!int.TryParse(line, out chart.Offset))
                        throw new FormatException(
                            "Malformed chart file: first line is not a valid integer offset.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                var part = line.Split(' ');
                var judgeLineIndex = part.Length > 1 ? int.Parse(part[1]) : -1;

                switch (part.First())
                {
                    case "bp":
                        chart.BpmList.Add(new Bpm
                        {
                            StartBeat = float.Parse(part[1]),
                            BeatPerMinute = float.Parse(part[2])
                        });
                        break;

                    case "cv":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].SpeedFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "cp":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].MoveFrames.Add(new MoveFrame
                        {
                            Beat = float.Parse(part[2]),
                            XValue = float.Parse(part[3]),
                            YValue = float.Parse(part[4])
                        });
                        break;

                    case "cd":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].RotateFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "ca":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].AlphaFrames.Add(new Frame
                        {
                            Beat = float.Parse(part[2]),
                            Value = float.Parse(part[3])
                        });
                        break;

                    case "cm":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].MoveEvents.Add(new MoveEvent
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndXValue = float.Parse(part[4]),
                            EndYValue = float.Parse(part[5]),
                            EasingType = new Easing(int.Parse(part[6]))
                        });
                        break;

                    case "cr":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].RotateEvents.Add(new Event
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndValue = float.Parse(part[4]),
                            EasingType = new Easing(int.Parse(part[5]))
                        });
                        break;

                    case "cf":
                        EnsureJudgeLineExists();
                        judgeDict[judgeLineIndex].AlphaEvents.Add(new Event
                        {
                            StartBeat = float.Parse(part[2]),
                            EndBeat = float.Parse(part[3]),
                            EndValue = float.Parse(part[4]),
                            EasingType = new Easing(1)
                        });
                        break;

                    default:
                        if (line.StartsWith("n"))
                        {
                            var noteType = (NoteType)int.Parse(part[0].Substring(1, 1));

                            // 检测边界情况：# 和 & 是否在同一行
                            string[] noteSpeedMultiplierPart;
                            string[] noteWidthRatioPart;

                            // 查找 # 和 & 在 part 中的索引
                            var hashIndex = Array.IndexOf(part, "#");
                            var ampIndex = Array.IndexOf(part, "&");

                            if (hashIndex != -1 && ampIndex != -1)
                            {
                                // 边界情况：# 和 & 都在同一行
                                noteSpeedMultiplierPart = new[] { "#", part[hashIndex + 1] };
                                noteWidthRatioPart = new[] { "&", part[ampIndex + 1] };
                            }
                            else
                            {
                                // 标准情况：# 和 & 各占一行
                                noteSpeedMultiplierPart = lines[i + 1].Split(' ');
                                noteWidthRatioPart = lines[i + 2].Split(' ');
                                i += 2;
                            }

                            var note = new Note
                            {
                                StartBeat = float.Parse(part[2]),
                                EndBeat = noteType == NoteType.Hold ? float.Parse(part[3]) : float.Parse(part[2]),
                                PositionX = float.Parse(part[noteType == NoteType.Hold ? 4 : 3]),
                                Above = part[noteType == NoteType.Hold ? 5 : 4] == "1",
                                IsFake = part[noteType == NoteType.Hold ? 6 : 5] == "1",
                                SpeedMultiplier = float.Parse(noteSpeedMultiplierPart[1]),
                                WidthRatio = float.Parse(noteWidthRatioPart[1]),
                                Type = noteType
                            };

                            EnsureJudgeLineExists();
                            judgeDict[judgeLineIndex].NoteList.Add(note);
                        }

                        break;
                }

                continue;

                // 本地函数：确保判定线存在
                void EnsureJudgeLineExists()
                {
                    if (!judgeDict.ContainsKey(judgeLineIndex))
                        judgeDict[judgeLineIndex] = new JudgeLine();
                }
            }

            foreach (var judgeLine in judgeDict.Values)
            {
                // 排序
                // Frame
                judgeLine.SpeedFrames = judgeLine.SpeedFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.MoveFrames = judgeLine.MoveFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.RotateFrames = judgeLine.RotateFrames.OrderBy(f => f.Beat).ToList();
                judgeLine.AlphaFrames = judgeLine.AlphaFrames.OrderBy(f => f.Beat).ToList();
                // Event
                judgeLine.MoveEvents = judgeLine.MoveEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.RotateEvents = judgeLine.RotateEvents.OrderBy(e => e.StartBeat).ToList();
                judgeLine.AlphaEvents = judgeLine.AlphaEvents.OrderBy(e => e.StartBeat).ToList();
                // Note
                judgeLine.NoteList = judgeLine.NoteList.OrderBy(n => n.StartBeat).ToList();
                // BPM
                chart.BpmList = chart.BpmList.OrderBy(b => b.StartBeat).ToList();
            }

            chart.JudgeLineList = judgeDict.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
            return chart;
        }

        /// <summary>
        /// 异步从PhiEditChart格式的字符串加载谱面
        /// </summary>
        /// <param name="pec">PhiEditChart字符串</param>
        /// <returns>已反序列化谱面</returns>
        public static async Task<Chart> LoadAsync(string pec)
            => await Task.Run(() => Load(pec));


        /// <summary>
        /// 导出PhiEditChart
        /// </summary>
        /// <returns>PhiEditChart</returns>
        public string Export()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Offset.ToString());
            foreach (var bpm in BpmList)
                stringBuilder.AppendLine(bpm.ToString());
            for (int i = 0; i < JudgeLineList.Count; i++)
            {
                var judgeLine = JudgeLineList[i];
                // Frame
                foreach (var moveFrame in judgeLine.MoveFrames)
                    stringBuilder.AppendLine(moveFrame.ToString(i));
                foreach (var speedFrame in judgeLine.SpeedFrames)
                    stringBuilder.AppendLine(speedFrame.ToString(i, "cv"));
                foreach (var rotateFrame in judgeLine.RotateFrames)
                    stringBuilder.AppendLine(rotateFrame.ToString(i, "cd"));
                foreach (var alphaFrame in judgeLine.AlphaFrames)
                    stringBuilder.AppendLine(alphaFrame.ToString(i, "ca"));
                // Event
                foreach (var moveEvent in judgeLine.MoveEvents)
                    stringBuilder.AppendLine(moveEvent.ToString(i));
                foreach (var rotateEvent in judgeLine.RotateEvents)
                    stringBuilder.AppendLine(rotateEvent.ToString(i, "cr"));
                foreach (var alphaEvent in judgeLine.AlphaEvents)
                    stringBuilder.AppendLine(alphaEvent.ToString(i, "cf"));
                // Note
                foreach (var note in judgeLine.NoteList)
                    stringBuilder.AppendLine(note.ToString(i));
            }

            return stringBuilder.ToString().Trim();
        }

        /// <summary>
        /// 异步导出PhiEditChart
        /// </summary>
        /// <returns>PhiEditChart</returns>
        public async Task<string> ExportAsync()
            => await Task.Run(Export);

        /// <summary>
        /// 流式导出PhiEditChart
        /// </summary>
        /// <param name="stream"></param>
        public void ExportToStream(Stream stream)
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            writer.WriteLine(Offset.ToString());
            foreach (var bpm in BpmList)
                writer.WriteLine(bpm.ToString());
            for (int i = 0; i < JudgeLineList.Count; i++)
            {
                var judgeLine = JudgeLineList[i];
                // Frame
                foreach (var moveFrame in judgeLine.MoveFrames)
                    writer.WriteLine(moveFrame.ToString(i));
                foreach (var speedFrame in judgeLine.SpeedFrames)
                    writer.WriteLine(speedFrame.ToString(i, "cv"));
                foreach (var rotateFrame in judgeLine.RotateFrames)
                    writer.WriteLine(rotateFrame.ToString(i, "cd"));
                foreach (var alphaFrame in judgeLine.AlphaFrames)
                    writer.WriteLine(alphaFrame.ToString(i, "ca"));
                // Event
                foreach (var moveEvent in judgeLine.MoveEvents)
                    writer.WriteLine(moveEvent.ToString(i));
                foreach (var rotateEvent in judgeLine.RotateEvents)
                    writer.WriteLine(rotateEvent.ToString(i, "cr"));
                foreach (var alphaEvent in judgeLine.AlphaEvents)
                    writer.WriteLine(alphaEvent.ToString(i, "cf"));
                // Note
                foreach (var note in judgeLine.NoteList)
                    writer.WriteLine(note.ToString(i));
                writer.Flush();
            }
        }

        /// <summary>
        /// 流式导出PhiEditChart
        /// </summary>
        /// <param name="stream"></param>
        public async Task ExportToStreamAsync(Stream stream)
        {
            await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            await writer.WriteLineAsync(Offset.ToString());
            foreach (var bpm in BpmList)
                await writer.WriteLineAsync(bpm.ToString());
            for (int i = 0; i < JudgeLineList.Count; i++)
            {
                var judgeLine = JudgeLineList[i];
                // Frame
                foreach (var moveFrame in judgeLine.MoveFrames)
                    await writer.WriteLineAsync(moveFrame.ToString(i));
                foreach (var speedFrame in judgeLine.SpeedFrames)
                    await writer.WriteLineAsync(speedFrame.ToString(i, "cv"));
                foreach (var rotateFrame in judgeLine.RotateFrames)
                    await writer.WriteLineAsync(rotateFrame.ToString(i, "cd"));
                foreach (var alphaFrame in judgeLine.AlphaFrames)
                    await writer.WriteLineAsync(alphaFrame.ToString(i, "ca"));
                // Event
                foreach (var moveEvent in judgeLine.MoveEvents)
                    await writer.WriteLineAsync(moveEvent.ToString(i));
                foreach (var rotateEvent in judgeLine.RotateEvents)
                    await writer.WriteLineAsync(rotateEvent.ToString(i, "cr"));
                foreach (var alphaEvent in judgeLine.AlphaEvents)
                    await writer.WriteLineAsync(alphaEvent.ToString(i, "cf"));
                // Note
                foreach (var note in judgeLine.NoteList)
                    await writer.WriteLineAsync(note.ToString(i));
                await writer.FlushAsync();
            }
        }
    }
}