using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Forestry.Flo.Services.Gis
{
    public static class JsonHelper
    {
        public static bool TryParseJson<T>(this string @this, out T result)
        {
            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => { success = false; args.ErrorContext.Handled = true; },
                MissingMemberHandling = MissingMemberHandling.Error,
            };
            result = JsonConvert.DeserializeObject<T>(@this, settings);
            return success;
        }
    }

    public class MicrosecondEpochConverter : DateTimeConverterBase
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((DateTime)value - _epoch).TotalMilliseconds + "000");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) { return null; }
            return _epoch.AddMilliseconds((long)reader.Value / 1000d);
        }
    }

    public class TokenStampConvertor : DateTimeConverterBase
    {
        private static readonly DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((DateTime)value - dt).TotalMilliseconds + "");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) { return null; }
            return dt.AddMilliseconds((long)reader.Value);
        }
    }
}
