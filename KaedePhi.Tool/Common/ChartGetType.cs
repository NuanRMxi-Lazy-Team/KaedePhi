using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace KaedePhi.Tool.Common;

public static class ChartGetType
{
    private const string UnsupportedChartMessage =
        "无法推断谱面类型，可能是因为谱面文本格式不正确或者不受支持的谱面类型。"
        + "请确保输入的谱面文本格式正确，并且是受支持的谱面类型之一。";

    /// <summary>
    /// 使用谱面文本，推算谱面类型，推算失败则抛出错误
    /// </summary>
    /// <param name="chartText"></param>
    /// <returns>类型</returns>
    /// <exception cref="NotSupportedException">输入了不支持的谱面类别</exception>
    [PublicAPI]
    [Pure]
    [Obsolete("请改用 GetType(TextReader)、GetType(Stream) 或 GetTypeAsync(Stream)。")]
    public static ChartType GetType(string chartText)
    {
        ArgumentNullException.ThrowIfNull(chartText);

        using var textReader = new StringReader(chartText);
        return GetType(textReader);
    }

    /// <summary>
    /// 从文本读取器中推算谱面类型，不缓存完整谱面结构。
    /// </summary>
    /// <param name="reader">待检测的文本读取器。</param>
    /// <returns>检测到的谱面类型。</returns>
    /// <exception cref="NotSupportedException">输入了不支持的谱面类别。</exception>
    [PublicAPI]
    public static ChartType GetType(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        using var jsonReader = CreateJsonReader(reader);
        try
        {
            return GetType(jsonReader);
        }
        catch (JsonException e)
        {
            throw new NotSupportedException(e.Message, e);
        }
    }

    /// <summary>
    /// 从输入流中推算谱面类型，只读取必要的令牌，不缓存完整谱面文本。
    /// </summary>
    /// <param name="stream">待检测的谱面输入流。</param>
    /// <returns>检测到的谱面类型。</returns>
    /// <exception cref="NotSupportedException">输入了不支持的谱面类别。</exception>
    [PublicAPI]
    public static ChartType GetType(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var textReader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true
        );
        return GetType(textReader);
    }

    /// <summary>
    /// 异步从输入流中推算谱面类型，只读取必要的令牌，不缓存完整谱面文本。
    /// </summary>
    /// <param name="stream">待检测的谱面输入流。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>检测到的谱面类型。</returns>
    /// <exception cref="NotSupportedException">输入了不支持的谱面类别。</exception>
    [PublicAPI]
    public static async Task<ChartType> GetTypeAsync(
        Stream stream,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var textReader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true
        );
        using var jsonReader = CreateJsonReader(textReader);
        try
        {
            return await GetTypeAsync(jsonReader, ct);
        }
        catch (JsonException e)
        {
            throw new NotSupportedException(e.Message, e);
        }
    }

    private static JsonTextReader CreateJsonReader(TextReader textReader) =>
        new(textReader)
        {
            DateParseHandling = DateParseHandling.None,
            CloseInput = false,
        };

    private static ChartType GetType(JsonTextReader reader)
    {
        if (!ReadNext(reader))
            throw new NotSupportedException(UnsupportedChartMessage);

        if (reader.TokenType == JsonToken.Integer)
            return ChartType.PhiEdit;
        if (reader.TokenType != JsonToken.StartObject)
            throw new NotSupportedException(UnsupportedChartMessage);

        var hasInfoObject = false;
        var hasLinesArray = false;
        var hasPhiChainFormat = false;
        var hasBpmList = false;
        var hasFormatVersion = false;
        var formatVersionValid = false;
        var formatVersion = 0;

        while (ReadNext(reader))
        {
            if (reader.TokenType == JsonToken.EndObject)
                break;
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonException("JSON 根对象包含非法属性令牌。");

            var propertyName = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
            if (!ReadNext(reader))
                throw new JsonException("JSON 属性缺少值。");

            switch (propertyName)
            {
                case "META" when reader.TokenType == JsonToken.StartObject:
                    return ChartType.RePhiEdit;
                case "formatVersion":
                    hasFormatVersion = true;
                    formatVersionValid = TryReadFormatVersion(reader, out formatVersion);
                    break;
                case "info":
                    hasInfoObject = reader.TokenType == JsonToken.StartObject;
                    break;
                case "lines":
                    hasLinesArray = reader.TokenType == JsonToken.StartArray;
                    break;
                case "format":
                    hasPhiChainFormat = IsIntegerValue(reader, 6UL);
                    break;
                case "bpm_list":
                    hasBpmList = reader.TokenType == JsonToken.StartArray;
                    break;
            }

            SkipValue(reader);
        }

        if (hasFormatVersion)
        {
            return !formatVersionValid
                ? throw new NotSupportedException(UnsupportedChartMessage)
                : GetTypeFromFormatVersion(formatVersion);
        }

        if (hasInfoObject && hasLinesArray)
            return ChartType.PhiFans;
        if (hasPhiChainFormat && hasBpmList)
            return ChartType.PhiChain;

        throw new NotSupportedException(UnsupportedChartMessage);
    }

    private static async Task<ChartType> GetTypeAsync(
        JsonTextReader reader,
        CancellationToken ct
    )
    {
        if (!await ReadNextAsync(reader, ct))
            throw new NotSupportedException(UnsupportedChartMessage);

        if (reader.TokenType == JsonToken.Integer)
            return ChartType.PhiEdit;
        if (reader.TokenType != JsonToken.StartObject)
            throw new NotSupportedException(UnsupportedChartMessage);

        var hasInfoObject = false;
        var hasLinesArray = false;
        var hasPhiChainFormat = false;
        var hasBpmList = false;
        var hasFormatVersion = false;
        var formatVersionValid = false;
        var formatVersion = 0;

        while (await ReadNextAsync(reader, ct))
        {
            if (reader.TokenType == JsonToken.EndObject)
                break;
            if (reader.TokenType != JsonToken.PropertyName)
                throw new JsonException("JSON 根对象包含非法属性令牌。");

            var propertyName = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
            if (!await ReadNextAsync(reader, ct))
                throw new JsonException("JSON 属性缺少值。");

            switch (propertyName)
            {
                case "META" when reader.TokenType == JsonToken.StartObject:
                    return ChartType.RePhiEdit;
                case "formatVersion":
                    hasFormatVersion = true;
                    formatVersionValid = TryReadFormatVersion(reader, out formatVersion);
                    break;
                case "info":
                    hasInfoObject = reader.TokenType == JsonToken.StartObject;
                    break;
                case "lines":
                    hasLinesArray = reader.TokenType == JsonToken.StartArray;
                    break;
                case "format":
                    hasPhiChainFormat = IsIntegerValue(reader, 6UL);
                    break;
                case "bpm_list":
                    hasBpmList = reader.TokenType == JsonToken.StartArray;
                    break;
            }

            await SkipValueAsync(reader, ct);
        }

        if (hasFormatVersion)
        {
            if (!formatVersionValid)
                throw new NotSupportedException(UnsupportedChartMessage);
            return GetTypeFromFormatVersion(formatVersion);
        }

        if (hasInfoObject && hasLinesArray)
            return ChartType.PhiFans;
        if (hasPhiChainFormat && hasBpmList)
            return ChartType.PhiChain;

        throw new NotSupportedException(UnsupportedChartMessage);
    }

    private static bool ReadNext(JsonTextReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.Comment)
                return true;
        }

        return false;
    }

    private static async Task<bool> ReadNextAsync(JsonTextReader reader, CancellationToken ct)
    {
        while (await reader.ReadAsync(ct))
        {
            if (reader.TokenType != JsonToken.Comment)
                return true;
        }

        return false;
    }

    private static void SkipValue(JsonTextReader reader)
    {
        if (reader.TokenType is not (JsonToken.StartObject or JsonToken.StartArray))
            return;

        var depth = 1;
        while (depth > 0)
        {
            if (!reader.Read())
                throw new JsonException("JSON 值未正常结束。");
            switch (reader.TokenType)
            {
                case JsonToken.StartObject or JsonToken.StartArray:
                    depth++;
                    break;
                case JsonToken.EndObject or JsonToken.EndArray:
                    depth--;
                    break;
            }
        }
    }

    private static async Task SkipValueAsync(JsonTextReader reader, CancellationToken ct)
    {
        if (reader.TokenType is not (JsonToken.StartObject or JsonToken.StartArray))
            return;

        var depth = 1;
        while (depth > 0)
        {
            if (!await reader.ReadAsync(ct))
                throw new JsonException("JSON 值未正常结束。");
            switch (reader.TokenType)
            {
                case JsonToken.StartObject or JsonToken.StartArray:
                    depth++;
                    break;
                case JsonToken.EndObject or JsonToken.EndArray:
                    depth--;
                    break;
            }
        }
    }

    private static bool TryReadFormatVersion(JsonTextReader reader, out int formatVersion)
    {
        formatVersion = 0;
        if (reader.TokenType != JsonToken.Integer)
            return false;

        var text = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
        return long.TryParse(
                   text,
                   NumberStyles.Integer,
                   CultureInfo.InvariantCulture,
                   out var parsed
               )
               && parsed is >= int.MinValue and <= int.MaxValue
               && (formatVersion = (int)parsed) == parsed;
    }

    private static bool IsIntegerValue(JsonTextReader reader, ulong expected)
    {
        if (reader.TokenType != JsonToken.Integer)
            return false;

        return ulong.TryParse(
            Convert.ToString(reader.Value, CultureInfo.InvariantCulture),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        ) && value == expected;
    }

    private static ChartType GetTypeFromFormatVersion(int formatVersion) =>
        formatVersion switch
        {
            1 => ChartType.PhigrosV1,
            3 => ChartType.PhigrosV3,
            // 哈？这是啥
            _ => throw new NotSupportedException(UnsupportedChartMessage),
        };
}