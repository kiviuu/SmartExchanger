using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartExchanger.Options
{
    internal static class GraphJsonOptions
    {
        public static JsonSerializerOptions Instance { get; } = Create();

        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
