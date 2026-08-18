using System.Text.Json;
using System.Text.Json.Nodes;

namespace C2C.DevicePlatform.Api.Services;

public sealed class SensitiveDataMaskingService
{
    private static readonly string[] SensitiveKeys =
    [
        "secret",
        "token",
        "password",
        "apikey",
        "api_key",
        "clientsecret",
        "privatekey"
    ];

    public string MaskJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var root = JsonNode.Parse(json);
            var masked = MaskNode(root, string.Empty);
            return masked?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? json;
        }
        catch (JsonException)
        {
            return MaskString(json);
        }
    }

    public string MaskString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (value.Length <= 8)
        {
            return "****";
        }

        return string.Concat(value.AsSpan(0, 4), "****", value.AsSpan(value.Length - 4));
    }

    private JsonNode? MaskNode(JsonNode? node, string propertyName)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var property in obj)
            {
                result[property.Key] = MaskNode(property.Value, property.Key);
            }

            return result;
        }

        if (node is JsonArray array)
        {
            var result = new JsonArray();
            foreach (var item in array)
            {
                result.Add(MaskNode(item, propertyName));
            }

            return result;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var textValue))
            {
                // Script payloads are blocked before masking checks.
                if (textValue.Contains("<script", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonValue.Create("[blocked-script-content]");
                }

                if (IsSensitiveProperty(propertyName) || LooksLikeSensitiveToken(textValue))
                {
                    return JsonValue.Create(MaskString(textValue));
                }

                return JsonValue.Create(textValue);
            }

            return JsonNode.Parse(jsonValue.ToJsonString());
        }

        return node;
    }

    private static bool IsSensitiveProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        return SensitiveKeys.Any(key => propertyName.Contains(key, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeSensitiveToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Count(character => character == '.') >= 2
            || value.Length >= 24;
    }
}
