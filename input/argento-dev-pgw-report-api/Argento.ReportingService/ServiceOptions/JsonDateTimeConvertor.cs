using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Argento.ReportingService.ServiceOptions
{
    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        string format = "yyyy-MM-ddTHH:mm:ss.fffK";

        /// <summary>
        /// Default format is yyyy-MM-ddTHH:mm:ss.fffK
        /// </summary>
        public JsonDateTimeConverter()
        {
        }

        /// <summary>
        /// Default format is yyyy-MM-ddTHH:mm:ss.fffK
        /// </summary>
        /// <param name="dateTimeFormat">default value is 'yyyy-MM-ddTHH:mm:ss.fffK'</param>
        public JsonDateTimeConverter(string dateTimeFormat)
        {
            this.format = dateTimeFormat;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // beleive for datatime from database always store with UTC
            // but DateTime Kind of data DateTime object from query with EF is Unspecific 
            // then force DateTime Kind from EF to UTC before convert to string format and write to response
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

            writer.WriteStringValue(value.ToString(format));
        }
    }
}
