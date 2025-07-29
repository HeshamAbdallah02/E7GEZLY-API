using System.Text.Json;
using System.Text.Json.Serialization;

namespace E7GEZLY_API.Converters
{
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;

            // Try parsing different formats
            if (TimeSpan.TryParse(value, out var timeSpan))
                return timeSpan;

            // Try parsing HH:mm format
            if (value.Contains(':') && value.Split(':').Length == 2)
            {
                return TimeSpan.Parse(value + ":00");
            }

            throw new JsonException($"Unable to parse '{value}' as TimeSpan");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm"));
            }
        }
    }
}