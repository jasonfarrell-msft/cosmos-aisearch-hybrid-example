using System.Text.Json;

namespace DTE.FunctionApp.Extensions
{
    public static class JsonElementExtensions
    {
        public static T GetPropertyValue<T>(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return property.Deserialize<T>() ?? throw new InvalidOperationException($"Property '{propertyName}' is not of type {typeof(T)}.");
            }

            throw new InvalidOperationException($"Property '{propertyName}' not found.");
        }

        public static string GetValueAsString(this JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => string.Empty,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetDouble().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => throw new InvalidOperationException($"Unsupported value kind: {element.ValueKind}")
            };
        }
    }
}