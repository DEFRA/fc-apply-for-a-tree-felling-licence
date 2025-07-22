using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Helpers;
public class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            long timestamp = (long)reader.Value;
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            return dateTime;
        }

        throw new JsonSerializationException("Expected Unix timestamp.");

    }

    public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
    {
        long timestamp = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        writer.WriteValue(timestamp);
    }
}