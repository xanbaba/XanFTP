using System.Text.Json;
using System.Text.Json.Serialization;

namespace XanFTP;

public class DynamicJsonConverter : JsonConverter<dynamic>
{
    public override dynamic? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                    return intValue;
                return reader.GetDouble();

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean();

            case JsonTokenType.StartObject:
                var dict = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(ref reader, options);
                return dict;

            case JsonTokenType.StartArray:
                var list = JsonSerializer.Deserialize<List<dynamic>>(ref reader, options);
                return list;

            case JsonTokenType.Null:
                return null;

            default:
                throw new JsonException($"Unexpected token type {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, dynamic value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType() ?? typeof(object), options);
    }
}