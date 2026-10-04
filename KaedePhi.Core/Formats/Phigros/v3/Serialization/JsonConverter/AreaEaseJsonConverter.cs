using System;
using System.Globalization;
using KaedePhi.Core.Formats.Phigros.v3.Model;
using Newtonsoft.Json;

namespace KaedePhi.Core.Formats.Phigros.v3.Serialization.JsonConverter
{
    /// <summary>
    /// 在噪域缓动类型与 JSON 整数编号之间转换。
    /// </summary>
    public sealed class AreaEaseJsonConverter : JsonConverter<AreaEase>
    {
        /// <summary>
        /// 将缓动类型写为原始整数编号。
        /// </summary>
        /// <param name="writer">目标 JSON 写入器。</param>
        /// <param name="value">待写入的缓动类型。</param>
        /// <param name="serializer">JSON 序列化器。</param>
        public override void WriteJson(JsonWriter writer, AreaEase value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value);
        }

        /// <summary>
        /// 从 JSON 整数编号读取缓动类型。
        /// </summary>
        /// <param name="reader">源 JSON 读取器。</param>
        /// <param name="objectType">目标类型。</param>
        /// <param name="existingValue">已有缓动类型。</param>
        /// <param name="hasExistingValue">是否存在已有值。</param>
        /// <param name="serializer">JSON 序列化器。</param>
        /// <returns>读取到的缓动类型。</returns>
        /// <exception cref="JsonSerializationException">JSON 值不是整数编号。</exception>
        public override AreaEase ReadJson(
            JsonReader reader,
            Type objectType,
            AreaEase existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType != JsonToken.Integer || reader.Value == null)
                throw new JsonSerializationException("噪域缓动类型必须是整数编号。");

            try
            {
                return new AreaEase(Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture));
            }
            catch (Exception exception) when (exception is FormatException || exception is OverflowException)
            {
                throw new JsonSerializationException("噪域缓动类型超出整数范围。", exception);
            }
        }
    }
}
