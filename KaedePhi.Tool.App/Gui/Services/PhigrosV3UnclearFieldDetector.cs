using System.Text;
using Newtonsoft.Json;

namespace KaedePhi.Tool.App.Gui.Services;

/// <summary>
/// 检测 PhigrosV3 谱面源文件中行为不明确的 BlockAreaList 字段是否含有外部导入内容。
/// </summary>
internal static class PhigrosV3UnclearFieldDetector
{
    // 官方谱面使用 blockAreaList，KaedePhi 序列化产物使用 BlockAreaList，两者均需匹配
    private const string BlockAreaListPropertyName = "blockAreaList";

    /// <summary>
    /// 判断谱面文件的 BlockAreaList 是否含有非默认内容。
    /// </summary>
    /// <param name="filePath">谱面文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>含有非默认内容时为 true。</returns>
    public static async Task<bool> HasNonDefaultBlockAreaListAsync(
        string filePath,
        CancellationToken ct = default
    )
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            useAsync: true
        );
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 65536);
        using var json = new JsonTextReader(reader);
        while (await json.ReadAsync(ct))
        {
            // 仅匹配顶层对象的字段，避免嵌套结构中同名属性造成误判
            if (
                json.TokenType != JsonToken.PropertyName
                || json.Depth != 1
                || !string.Equals(
                    json.Value?.ToString(),
                    BlockAreaListPropertyName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                continue;

            if (!await json.ReadAsync(ct))
                return false;

            // 空数组与 null 均视为默认值，其余内容都来自外部导入
            if (json.TokenType != JsonToken.StartArray)
                return json.TokenType != JsonToken.Null;
            return await json.ReadAsync(ct) && json.TokenType != JsonToken.EndArray;
        }

        return false;
    }
}
