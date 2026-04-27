using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KaedePhi.Tool.Common;

public static class ChartGetType
{
    private const string UnsupportedChartMessage =
        "无法推断谱面类型，可能是因为谱面文本格式不正确或者不受支持的谱面类型。" +
        "请确保输入的谱面文本格式正确，并且是受支持的谱面类型之一。";

    /// <summary>
    /// 使用谱面文本，推算谱面类型，推算失败则抛出错误
    /// </summary>
    /// <param name="chartText"></param>
    /// <returns>类型</returns>
    /// <exception cref="NotSupportedException">输入了不支持的谱面类别</exception>
    [PublicAPI]
    [Pure]
    public static ChartType GetType(string chartText)
    {
        // 尝试校验是否是一个json文件，如果不是一个json文件，则一定是PhiEdit
        if (!chartText.TrimStart().StartsWith('{'))
        {
            // 也不一定，如果第一行不是纯数字，那么这就是个无效文件
            if (int.TryParse(chartText.Split('\n')[0].Trim(), out _))
                return ChartType.PhiEdit;

            throw new NotSupportedException(UnsupportedChartMessage);
        }

        // 看起来是一个json文件，序列化为dynamic对象，按特征进行读取
        try
        {
            dynamic jsonObj = JsonConvert.DeserializeObject(chartText) ??
                              throw new ArgumentNullException(nameof(chartText), "啊拉？序列化失败了...");

            // 如果存在META字段在根目录，且此字段为一个JsonObject，则这是一个RePhiEdit谱面
            if (jsonObj.META is JObject)
                return ChartType.RePhiEdit;

            // 如果存在formatVersion字段，且字段类型为int，则根据版本号判断PhigrosV1/V3谱面
            if (jsonObj.formatVersion is JValue
                {
                    Type: JTokenType.Integer or JTokenType.Float
                })
                return GetTypeFromFormatVersion((int)jsonObj.formatVersion);

            // 如果存在info字段的同时，info字段为jsonObject，且存在lines字段，且lines字段为JsonArray，则这是PhiFans谱面
            if (jsonObj.info is JObject && jsonObj.lines != null && jsonObj.lines is JArray)
                return ChartType.PhiFans;
        }
        catch (Exception e)
        {
            // 附加原始异常的同时包装NotSupportedException
            throw new NotSupportedException(e.Message);
        }

        throw new NotSupportedException(UnsupportedChartMessage);
    }

    private static ChartType GetTypeFromFormatVersion(int formatVersion) => formatVersion switch
    {
        1 => ChartType.PhigrosV1,
        3 => ChartType.PhigrosV3,
        // 哈？这是啥
        _ => throw new NotSupportedException(UnsupportedChartMessage)
    };
}